import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { ApiError, UserListItemDto } from '../types';

/** Clears a login lockout early (POST /api/v1/users/{id}/unlock). */
export function useUnlockUser() {
  const queryClient = useQueryClient();

  return useMutation<UserListItemDto, ApiError, { id: string }>({
    mutationFn: async ({ id }) => {
      const { data } = await api.post<{ data: UserListItemDto }>(`/api/v1/users/${id}/unlock`);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.lists() });
    },
  });
}
