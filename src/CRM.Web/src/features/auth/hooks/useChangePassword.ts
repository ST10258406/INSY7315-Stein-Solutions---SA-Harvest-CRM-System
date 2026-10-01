import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import type { ChangePasswordFormValues } from '../schemas/changePasswordSchema';

export function useChangePassword() {
  return useMutation({
    mutationFn: async (values: ChangePasswordFormValues) => {
      await api.patch('/api/auth/change-password', values);
    },
    onSuccess: () => {
      // The server keeps this session alive and clears the flag, so the user simply
      // continues into the app — no second login.
      const { user, setUser } = useAuthStore.getState();
      if (user) setUser({ ...user, mustChangePassword: false });
    },
  });
}
