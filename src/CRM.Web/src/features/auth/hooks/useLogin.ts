import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import type { LoginFormValues } from '../schemas/loginSchema';
import type { LoginResponseDto } from '@/services/authService';

export function useLogin() {
  const login = useAuthStore((s) => s.login);

  return useMutation({
    mutationFn: async (values: LoginFormValues) => {
      const { data } = await api.post<LoginResponseDto>('/api/auth/login', values);
      return data;
    },
    onSuccess: (data) => {
      login(data);
    },
  });
}