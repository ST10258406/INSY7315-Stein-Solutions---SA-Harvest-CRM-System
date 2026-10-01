import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';
import type { SessionResponseDto } from '@/services/authService';

// The API requires this header on the cookie-authenticated auth endpoints (refresh,
// logout) as CSRF protection: a cross-site page can't add a custom header without a CORS
// preflight, which only our own origin passes.
const CSRF_HEADERS = { 'X-Requested-With': 'XMLHttpRequest' } as const;

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL, // e.g. http://localhost:5000
  // Send/receive the HttpOnly refresh cookie (path /api/auth) across the SPA→API origin.
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    ...CSRF_HEADERS,
  },
});

export const publicApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const accessToken = useAuthStore.getState().accessToken;

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }

  return config;
});

let inflightRefresh: Promise<SessionResponseDto> | null = null;

/**
 * Exchanges the HttpOnly refresh cookie for a new access token (and a rotated cookie).
 * Used both on page load (useHydrateAuth) and when a request 401s. Concurrent callers
 * share one request: every refresh rotates the cookie, so firing two at once would
 * present the same cookie twice.
 *
 * Uses a bare axios call (not `api`) so a failure here never re-enters the 401
 * interceptor below.
 */
export function refreshSession(): Promise<SessionResponseDto> {
  inflightRefresh ??= axios
    .post<SessionResponseDto>(`${import.meta.env.VITE_API_BASE_URL}/api/auth/refresh`, null, {
      withCredentials: true,
      headers: CSRF_HEADERS,
    })
    .then(({ data }) => {
      useAuthStore.getState().login(data);
      return data;
    })
    .finally(() => {
      inflightRefresh = null;
    });

  return inflightRefresh;
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined;

    // The server says this account must replace its temporary password first. Flag it in the
    // store; ProtectedRoute then redirects to the change-password screen.
    if (
      error.response?.status === 403 &&
      (error.response.data as { code?: string } | undefined)?.code === 'PASSWORD_CHANGE_REQUIRED'
    ) {
      const { user, setUser } = useAuthStore.getState();
      if (user && !user.mustChangePassword) setUser({ ...user, mustChangePassword: true });
      return Promise.reject(error);
    }

    // Only try to recover from 401s, and only once per request.
    if (!originalRequest || error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Dev-only landing-page bypass (authStore.enableDevBypass) has no real
    // token, so every request 401s. Without this, the first one would hit the
    // refresh attempt below, fail (no refresh cookie either), and force-logout
    // back to /login — defeating the bypass immediately. Let the caller's own
    // error handling (query isError states, etc.) deal with it instead.
    if (useAuthStore.getState().isDevBypass) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      // Rejected outright if the session is over — expired, logged out elsewhere, or the
      // whole token family revoked by reuse detection.
      const { accessToken } = await refreshSession();
      originalRequest.headers.Authorization = `Bearer ${accessToken}`;
      return api(originalRequest);
    } catch (refreshError) {
      useAuthStore.getState().logout();
      window.location.href = paths.login;
      return Promise.reject(refreshError);
    }
  }
);
