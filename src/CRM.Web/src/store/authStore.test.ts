import { describe, it, expect, beforeEach } from 'vitest';
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
});
