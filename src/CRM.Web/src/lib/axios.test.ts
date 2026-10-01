import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import axios from 'axios';
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
