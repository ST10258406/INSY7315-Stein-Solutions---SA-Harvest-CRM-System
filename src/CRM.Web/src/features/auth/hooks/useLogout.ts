import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';

export const LOGOUT_FAILED_MESSAGE = "Couldn't sign you out. Check your connection and try again.";

export function useLogout() {
  const clearAuth = useAuthStore((s) => s.logout);
  const navigate = useNavigate();

  const finishLogout = () => {
    clearAuth();
    navigate(paths.login, { replace: true });
  };

  return useMutation({
    mutationFn: async () => {
      // The refresh token travels in the HttpOnly cookie, which JavaScript can't delete —
      // only the API's response can revoke it and clear the cookie.
      await api.post('/api/auth/logout');
    },
    onSuccess: finishLogout,
    onError: () => {
      // Session already over server-side: the 401 interceptor tried to refresh, failed (and
      // the API deleted the dead cookie), then signed us out itself. Nothing is left live.
      if (!useAuthStore.getState().isAuthenticated) {
        finishLogout();
        return;
      }

      // Otherwise the request never got a real answer (offline, server error). Don't pretend
      // the user is signed out: the cookie is still live, so a reload would sign them straight
      // back in — unsafe on a shared computer. Stay signed in and let them retry.
      toast.error(LOGOUT_FAILED_MESSAGE);
    },
  });
}
