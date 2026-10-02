import { useEffect } from 'react';
import { refreshSession } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';

/**
 * Restores the session after a page reload. The access token lived only in memory and is
 * gone, but the browser still holds the HttpOnly refresh cookie — so ask the API for a
 * fresh access token. No cookie / expired session → stay logged out, without redirecting
 * (public pages like the landing page and donor form also run this).
 */
export function useHydrateAuth() {
  const setHydrating = useAuthStore((s) => s.setHydrating);

  useEffect(() => {
    refreshSession()
      .catch(() => {
        // Not signed in — that's a normal state, not an error.
      })
      .finally(() => setHydrating(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
}
