import { useEffect } from 'react';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';

export function useHydrateAuth() {
  const setUser = useAuthStore((s) => s.setUser);
  const setHydrating = useAuthStore((s) => s.setHydrating);
  const accessToken = useAuthStore((s) => s.accessToken);

  useEffect(() => {
    async function hydrate() {
      if (!accessToken) {
        setHydrating(false);
        return;
      }

      try {
        const { data } = await api.get('/api/users/me');
        setUser(data); // Assuming a flat response like the login DTO
      } catch {
        // Token's no good — the axios interceptor will already have
        // attempted a refresh and logged out if that failed too.
      } finally {
        setHydrating(false);
      }
    }

    hydrate();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
}
