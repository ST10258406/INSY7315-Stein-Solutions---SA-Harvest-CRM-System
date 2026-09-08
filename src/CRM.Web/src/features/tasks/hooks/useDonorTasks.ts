import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { taskKeys } from './taskKeys';
import type { ApiError, DonorTasksFilters, PaginatedResult, TaskDto } from '../types';

/**
 * GET /api/v1/donors/{id}/tasks — every task for one donor (tri-state
 * `isCompleted` filter). Returns the `PaginatedResult` envelope directly.
 */
export function useDonorTasks(donorId: string | undefined, filters: DonorTasksFilters = {}) {
  return useQuery<PaginatedResult<TaskDto>, ApiError>({
    queryKey: taskKeys.donor(donorId, filters),
    enabled: !!donorId,
    queryFn: async () => {
      const { data } = await api.get<PaginatedResult<TaskDto>>(`/api/v1/donors/${donorId}/tasks`, {
        params: filters,
      });
      return data;
    },
    placeholderData: keepPreviousData,
  });
}
