import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { notificationKeys } from './notificationKeys';
import type { ApiError } from '../types';

/**
 * PATCH /api/v1/notifications/{id}/read — 204. 403 if the notification belongs to
 * another user, 404 if it is missing. Invalidates the bell on success.
 */
export function useMarkNotificationRead() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: async (id) => {
      await api.patch(`/api/v1/notifications/${id}/read`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}
