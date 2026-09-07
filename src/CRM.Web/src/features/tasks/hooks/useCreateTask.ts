import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { taskKeys } from './taskKeys';
import type { ApiError, CreateTaskRequest, TaskDto } from '../types';

export interface CreateTaskArgs {
  donorId: string;
  request: CreateTaskRequest;
}

/**
 * POST /api/v1/donors/{donorId}/tasks. Fires a `TaskAssigned` notification
 * server-side. `donorId` is a mutation argument, not a hook argument, because
 * the "New Task" flow on the My Tasks page picks the donor inside the form —
 * binding it at hook-construction time would POST to `/donors//tasks`.
 * On success, refresh every task list plus that donor's detail (task counts).
 */
export function useCreateTask() {
  const queryClient = useQueryClient();

  return useMutation<TaskDto, ApiError, CreateTaskArgs>({
    mutationFn: async ({ donorId, request }) => {
      const { data } = await api.post<{ data: TaskDto }>(`/api/v1/donors/${donorId}/tasks`, request);
      return data.data;
    },
    onSuccess: (_task, { donorId }) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(donorId) });
    },
  });
}
