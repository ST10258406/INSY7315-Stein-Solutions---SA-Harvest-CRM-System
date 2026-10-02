import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { ApiError, UserListItemDto } from '../types';

export function useSetUserActiveStatus() {
  const queryClient = useQueryClient();

  return useMutation<UserListItemDto, ApiError, { id: string; isActive: boolean }>({
    mutationFn: async ({ id, isActive }) => {
      const { data } = await api.patch<{ data: UserListItemDto }>(`/api/v1/users/${id}/status`, { isActive });
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() });
    },
  });
}
