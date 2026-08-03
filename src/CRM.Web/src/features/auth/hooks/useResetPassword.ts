import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ResetPasswordFormValues } from '../schemas/resetPasswordSchema';
import type { ResetPasswordResponseDto } from '@/services/authService';

export interface ResetPasswordPayload extends ResetPasswordFormValues {
  token: string;
  email: string;
}

export function useResetPassword() {
  return useMutation({
    mutationFn: async (values: ResetPasswordPayload) => {
      const { data } = await api.post<ResetPasswordResponseDto>('/api/auth/reset-password', values);
      return data;
    },
  });
}
