import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { taskKeys } from './taskKeys';
import type { ApiError, MyTasksFilters, PaginatedResult, TaskDto } from '../types';

/**
 * GET /api/v1/tasks — the current user's assigned tasks (assignee comes from the
 * JWT). Returns the `PaginatedResult` envelope directly (no `{ data }` wrapper).
 */
export function useMyTasks(filters: MyTasksFilters = {}) {
  return useQuery<PaginatedResult<TaskDto>, ApiError>({
    queryKey: taskKeys.mine(filters),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResult<TaskDto>>('/api/v1/tasks', { params: filters });
      return data;
    },
    placeholderData: keepPreviousData,
  });
}
