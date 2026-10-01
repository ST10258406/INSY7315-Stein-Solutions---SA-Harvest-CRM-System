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
  // True only for the landing page's "To CRM" dev shortcut (see enableDevBypass).
  // Lets ProtectedRoute through with no real token; axios.ts checks this flag
  // to skip its refresh/logout dance, since every real API call will 401.
  isDevBypass: boolean;

  /** Sets the session from a login or refresh response. */
  login: (result: SessionResponseDto) => void;
  logout: () => void;
  setAccessToken: (token: string) => void;
  setUser: (user: UserSummaryDto) => void;
  setHydrating: (value: boolean) => void;
  enableDevBypass: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isHydrating: true, // starts true — see hydration section below
  isDevBypass: false,

  login: (result) =>
    set({
      user: result.user,
      accessToken: result.accessToken,
      isAuthenticated: true,
      isDevBypass: false,
    }),

  logout: () =>
    set({
      user: null,
      accessToken: null,
      isAuthenticated: false,
      isDevBypass: false,
    }),

  setAccessToken: (token) => set({ accessToken: token }),

  setUser: (user) => set({ user, isAuthenticated: true, isDevBypass: false }),

  setHydrating: (value) => set({ isHydrating: value }),

  // Landing page "To CRM" button, development builds only. No real token is
  // issued — every API call still 401s — this only satisfies ProtectedRoute
  // so the CRM shell/navigation is reachable without going through /login.
  // `import.meta.env.DEV` is statically false in a production build, so
  // Rollup dead-code-eliminates this entire branch from the prod bundle:
  // this can't become a live no-op guard that silently regresses later,
  // it structurally cannot run outside a dev server.
  enableDevBypass: () => {
    if (!import.meta.env.DEV) return;
    set({
      isAuthenticated: true,
      isDevBypass: true,
      user: {
        id: 'dev-bypass',
        firstName: 'Dev',
        lastName: 'Bypass',
        email: 'dev-bypass@local',
        roles: ['SuperAdmin', 'Admin'],
      },
    });
  },
}));
