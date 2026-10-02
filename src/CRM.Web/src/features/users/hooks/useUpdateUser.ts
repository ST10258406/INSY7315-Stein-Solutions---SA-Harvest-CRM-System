import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { ApiError, UpdateUserRequest, UserListItemDto } from '../types';

export function useUpdateUser() {
  const queryClient = useQueryClient();

  return useMutation<UserListItemDto, ApiError, { id: string; request: UpdateUserRequest }>({
    mutationFn: async ({ id, request }) => {
      const { data } = await api.patch<{ data: UserListItemDto }>(`/api/v1/users/${id}`, request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() });
    },
  });
}
