import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import axios, { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { api, refreshSession } from './axios';
import { useAuthStore } from '@/store/authStore';

const session = {
  accessToken: 'restored-access-token',
  expiresIn: 3600,
  user: { id: 'u1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@saharvest.org', roles: ['Admin'] },
};

describe('refreshSession (HttpOnly cookie session restore)', () => {
  beforeEach(() => {
    useAuthStore.setState({ user: null, accessToken: null, isAuthenticated: false, isHydrating: true, isDevBypass: false });
  });

  afterEach(() => vi.restoreAllMocks());

  it('sends the cookie with the CSRF header and no body, then restores the session', async () => {
    const post = vi.spyOn(axios, 'post').mockResolvedValue({ data: session });

    await refreshSession();

    expect(post).toHaveBeenCalledTimes(1);
    const [url, body, config] = post.mock.calls[0];
    expect(url).toMatch(/\/api\/auth\/refresh$/);
    expect(body).toBeNull(); // the refresh token is never in JavaScript
    expect(config?.withCredentials).toBe(true);
    expect(config?.headers).toMatchObject({ 'X-Requested-With': 'XMLHttpRequest' });

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.accessToken).toBe('restored-access-token');
    expect(state.user?.email).toBe('ada@saharvest.org');
  });

  it('shares one request between concurrent callers, since each refresh rotates the cookie', async () => {
    let resolve!: (value: unknown) => void;
    const post = vi.spyOn(axios, 'post').mockReturnValue(new Promise((r) => (resolve = r)));

    const first = refreshSession();
    const second = refreshSession();
    resolve({ data: session });
    await Promise.all([first, second]);

    expect(post).toHaveBeenCalledTimes(1);
  });

  it('leaves the store signed out when there is no valid cookie', async () => {
    vi.spyOn(axios, 'post').mockRejectedValue(new Error('401'));

    await expect(refreshSession()).rejects.toThrow();

    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});

describe('api client', () => {
  it('sends credentials (the refresh cookie) and the CSRF header by default', () => {
    expect(api.defaults.withCredentials).toBe(true);
    expect(api.defaults.headers['X-Requested-With']).toBe('XMLHttpRequest');
  });
});

describe('api client response interceptor', () => {
  const originalAdapter = api.defaults.adapter;

  // A fake transport: requests carrying the stale token get a 401, the refreshed token gets a 200.
  function installAdapter(respond: (config: InternalAxiosRequestConfig) => { status: number; data?: unknown }) {
    api.defaults.adapter = async (config) => {
      const { status, data } = respond(config);
      const response = { data: data ?? {}, status, statusText: '', headers: {}, config } as AxiosResponse;
      if (status >= 400) {
        throw new AxiosError('failed', String(status), config, undefined, response);
      }
      return response;
    };
  }

  beforeEach(() => {
    useAuthStore.setState({
      user: session.user,
      accessToken: 'stale-token',
      isAuthenticated: true,
      isHydrating: false,
      isDevBypass: false,
    });
  });

  afterEach(() => {
    api.defaults.adapter = originalAdapter;
    vi.restoreAllMocks();
  });

  it('refreshes on 401 and retries the original request with the new token', async () => {
    const post = vi.spyOn(axios, 'post').mockResolvedValue({ data: session });
    installAdapter((config) =>
      config.headers.Authorization === 'Bearer restored-access-token' ? { status: 200, data: { ok: true } } : { status: 401 },
    );

    const response = await api.get('/api/v1/donors');

    expect(response.data).toEqual({ ok: true });
    expect(post).toHaveBeenCalledTimes(1);
  });

  it('does not stampede the refresh endpoint when several requests 401 together', async () => {
    const post = vi.spyOn(axios, 'post').mockResolvedValue({ data: session });
    installAdapter((config) =>
      config.headers.Authorization === 'Bearer restored-access-token' ? { status: 200 } : { status: 401 },
    );

    await Promise.all([api.get('/api/v1/donors'), api.get('/api/v1/tasks'), api.get('/api/v1/approvals')]);

    expect(post).toHaveBeenCalledTimes(1);
  });

  it('flags the user for a forced password change on 403 PASSWORD_CHANGE_REQUIRED, without refreshing', async () => {
    const post = vi.spyOn(axios, 'post');
    installAdapter(() => ({ status: 403, data: { code: 'PASSWORD_CHANGE_REQUIRED' } }));

    await expect(api.get('/api/v1/donors')).rejects.toBeDefined();

    expect(post).not.toHaveBeenCalled();
    expect(useAuthStore.getState().user?.mustChangePassword).toBe(true);
  });
});
