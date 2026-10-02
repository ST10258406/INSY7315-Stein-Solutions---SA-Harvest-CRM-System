import { create } from 'zustand';
import type { UserSummaryDto, SessionResponseDto } from '@/services/authService';

// Token Storage Strategy Decision
// - Access token: memory only (this store). No localStorage/sessionStorage, ever.
// - Refresh token: never visible to JavaScript at all. The API sets it as an HttpOnly,
//   Secure cookie scoped to /api/auth, so XSS can't read or exfiltrate it.
// Surviving a page reload: on app start useHydrateAuth calls POST /api/auth/refresh; the
// browser attaches the cookie, and the response restores the access token and user here.

interface AuthState {
  user: UserSummaryDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isHydrating: boolean;
  // Lets ProtectedRoute through with no real token; axios.ts checks this flag
  // to skip its refresh/logout dance, since every real API call will 401.

  /** Sets the session from a login or refresh response. */
  login: (result: SessionResponseDto) => void;
  logout: () => void;
  setAccessToken: (token: string) => void;
  setUser: (user: UserSummaryDto) => void;
  setHydrating: (value: boolean) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isHydrating: true, // starts true — see hydration section below

  login: (result) =>
    set({
      user: result.user,
      accessToken: result.accessToken,
      isAuthenticated: true,
    }),

  logout: () =>
    set({
      user: null,
      accessToken: null,
      isAuthenticated: false,
    }),

  setAccessToken: (token) => set({ accessToken: token }),

  setUser: (user) => set({ user, isAuthenticated: true }),

  setHydrating: (value) => set({ isHydrating: value }),
}));
