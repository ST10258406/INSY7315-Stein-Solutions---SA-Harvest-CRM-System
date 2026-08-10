import { create } from 'zustand';
import type { UserSummaryDto, LoginResponseDto } from '@/services/authService';

// Token Storage Strategy Decision: Memory-only (No persistence)
// Decision: Access tokens and refresh tokens are stored in memory only.
// Rationale: This prevents XSS attacks from easily extracting tokens from localStorage/sessionStorage.
// Hydration relies on a `/api/users/me` check on application load if an active session exists.

interface AuthState {
  user: UserSummaryDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isHydrating: boolean;

  login: (result: LoginResponseDto) => void;
  logout: () => void;
  setAccessToken: (token: string) => void;
  setUser: (user: UserSummaryDto) => void;
  setHydrating: (value: boolean) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isHydrating: true, // starts true — see hydration section below

  login: (result) =>
    set({
      user: result.user,
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      isAuthenticated: true,
    }),

  logout: () =>
    set({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
    }),

  setAccessToken: (token) => set({ accessToken: token }),

  setUser: (user) => set({ user, isAuthenticated: true }),

  setHydrating: (value) => set({ isHydrating: value }),
}));
