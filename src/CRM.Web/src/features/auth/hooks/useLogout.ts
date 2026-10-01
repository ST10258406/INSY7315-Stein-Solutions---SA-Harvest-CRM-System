import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';

export function useLogout() {
  const clearAuth = useAuthStore((s) => s.logout);
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async () => {
      // The refresh token travels in the HttpOnly cookie; the API revokes it and clears
      // the cookie, so a reload afterwards stays logged out.
      await api.post('/api/auth/logout');
    },
    onSettled: () => {
      // Clear local session and redirect even if the API call failed
      // (e.g. token already expired) — the user still expects to be logged out.
      clearAuth();
      navigate(paths.login, { replace: true });
    },
  });
}
