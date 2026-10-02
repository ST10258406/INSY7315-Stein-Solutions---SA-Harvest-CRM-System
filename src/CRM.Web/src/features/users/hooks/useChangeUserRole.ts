import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { ApiError, ChangeUserRoleRequest, UserListItemDto } from '../types';

export function useChangeUserRole() {
  const queryClient = useQueryClient();

  return useMutation<UserListItemDto, ApiError, { id: string; request: ChangeUserRoleRequest }>({
    mutationFn: async ({ id, request }) => {
      const { data } = await api.patch<{ data: UserListItemDto }>(`/api/v1/users/${id}/role`, request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() });
    },
  });
}
