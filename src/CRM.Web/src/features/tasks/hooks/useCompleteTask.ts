import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { taskKeys } from './taskKeys';
import type { ApiError } from '../types';

/**
 * POST /api/v1/tasks/{id}/complete — 204. Any authenticated user may complete a
 * task (not just the assignee). 400 if it is already complete.
 */
export function useCompleteTask() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: async (taskId) => {
      await api.post(`/api/v1/tasks/${taskId}/complete`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}
