import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { notificationKeys } from './notificationKeys';
import type { ApiError } from '../types';

/** PATCH /api/v1/notifications/read-all — 204. Bulk-marks every unread as read. */
export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, void>({
    mutationFn: async () => {
      await api.patch('/api/v1/notifications/read-all');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}
