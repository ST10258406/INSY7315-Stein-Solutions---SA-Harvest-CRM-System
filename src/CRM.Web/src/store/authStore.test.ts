import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { useAuthStore } from './authStore';

describe('authStore', () => {
  const mockUser = {
    id: '1',
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com',
    roles: ['Admin'],
  };

  const mockLoginResult = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresIn: 3600,
    user: mockUser,
  };

  beforeEach(() => {
    // Reset store to initial state before each test
    useAuthStore.setState({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isHydrating: true,
      isDevBypass: false,
    });
  });

  it('Store initial state has isAuthenticated: false and user: null', () => {
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(state.isHydrating).toBe(true);
  });

  it('login() sets all four fields correctly', () => {
    useAuthStore.getState().login(mockLoginResult);
    const state = useAuthStore.getState();

    expect(state.user).toEqual(mockUser);
    expect(state.accessToken).toBe('access-token');
    expect(state.refreshToken).toBe('refresh-token');
    expect(state.isAuthenticated).toBe(true);
  });

  it('logout() clears all four fields back to their initial state', () => {
    useAuthStore.getState().login(mockLoginResult);
    useAuthStore.getState().logout();
    const state = useAuthStore.getState();

    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });

  it('setAccessToken() updates only the access token, leaves user and refreshToken untouched', () => {
    useAuthStore.getState().login(mockLoginResult);
    useAuthStore.getState().setAccessToken('new-access-token');
    const state = useAuthStore.getState();

    expect(state.accessToken).toBe('new-access-token');
    expect(state.user).toEqual(mockUser);
    expect(state.refreshToken).toBe('refresh-token');
    expect(state.isAuthenticated).toBe(true);
  });

  it('setUser() sets the user and flips isAuthenticated to true', () => {
    useAuthStore.getState().setUser(mockUser);
    const state = useAuthStore.getState();

    expect(state.user).toEqual(mockUser);
    expect(state.isAuthenticated).toBe(true);
  });

  describe('enableDevBypass (landing page "To CRM" dev shortcut)', () => {
    afterEach(() => vi.unstubAllEnvs());

    it('sets isAuthenticated, isDevBypass, and a synthetic user — but never a real token', () => {
      useAuthStore.getState().enableDevBypass();
      const state = useAuthStore.getState();

      expect(state.isAuthenticated).toBe(true);
      expect(state.isDevBypass).toBe(true);
      expect(state.user).not.toBeNull();
      expect(state.accessToken).toBeNull();
      expect(state.refreshToken).toBeNull();
    });

    it('is a no-op outside a dev build (import.meta.env.DEV === false)', () => {
      vi.stubEnv('DEV', false);

      useAuthStore.getState().enableDevBypass();
      const state = useAuthStore.getState();

      expect(state.isAuthenticated).toBe(false);
      expect(state.isDevBypass).toBe(false);
      expect(state.user).toBeNull();
    });

    it('logout() clears isDevBypass along with everything else', () => {
      useAuthStore.getState().enableDevBypass();
      useAuthStore.getState().logout();
      const state = useAuthStore.getState();

      expect(state.isDevBypass).toBe(false);
      expect(state.isAuthenticated).toBe(false);
    });

    it('a real login() also clears any stale isDevBypass flag', () => {
      useAuthStore.getState().enableDevBypass();
      useAuthStore.getState().login(mockLoginResult);
      const state = useAuthStore.getState();

      expect(state.isDevBypass).toBe(false);
      expect(state.accessToken).toBe('access-token');
    });
  });
});
