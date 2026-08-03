import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ForgotPasswordFormValues } from '../schemas/forgotPasswordSchema';
import type { ForgotPasswordResponseDto } from '@/services/authService';

export function useForgotPassword() {
  return useMutation({
    mutationFn: async (values: ForgotPasswordFormValues) => {
      const { data } = await api.post<ForgotPasswordResponseDto>('/api/auth/forgot-password', values);
      return data;
    },
  });
}
