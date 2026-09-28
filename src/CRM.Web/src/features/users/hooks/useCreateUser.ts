import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { ApiError, CreateUserRequest, CreateUserResponseDto } from '../types';

export function useCreateUser() {
  const queryClient = useQueryClient();

  return useMutation<CreateUserResponseDto, ApiError, CreateUserRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: CreateUserResponseDto }>('/api/v1/users', request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() });
    },
  });
}
