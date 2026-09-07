import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { taskKeys } from './taskKeys';
import type { ApiError, CreateTaskRequest, TaskDto } from '../types';

/**
 * POST /api/v1/donors/{id}/tasks. Fires a `TaskAssigned` notification server-side.
 * On success, refresh every task list plus the donor detail (task counts).
 */
export function useCreateTask(donorId: string) {
  const queryClient = useQueryClient();

  return useMutation<TaskDto, ApiError, CreateTaskRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: TaskDto }>(`/api/v1/donors/${donorId}/tasks`, request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(donorId) });
    },
  });
}
