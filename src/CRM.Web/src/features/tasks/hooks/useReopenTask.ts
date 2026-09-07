import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { taskKeys } from './taskKeys';
import type { ApiError } from '../types';

/**
 * POST /api/v1/tasks/{id}/reopen — 204. Procurement+ only (asymmetric with
 * complete, which any authenticated user can do). 400 if the task is not completed.
 */
export function useReopenTask() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: async (taskId) => {
      await api.post(`/api/v1/tasks/${taskId}/reopen`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}
