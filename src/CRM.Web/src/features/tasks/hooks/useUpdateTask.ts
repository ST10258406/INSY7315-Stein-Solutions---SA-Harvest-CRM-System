import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { taskKeys } from './taskKeys';
import type { ApiError, TaskDto, UpdateTaskRequest } from '../types';

/** PATCH /api/v1/tasks/{id} — partial update of a task. Role: Procurement+. */
export function useUpdateTask(taskId: string) {
  const queryClient = useQueryClient();

  return useMutation<TaskDto, ApiError, UpdateTaskRequest>({
    mutationFn: async (request) => {
      const { data } = await api.patch<{ data: TaskDto }>(`/api/v1/tasks/${taskId}`, request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}
