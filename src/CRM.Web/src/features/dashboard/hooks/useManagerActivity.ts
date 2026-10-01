import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError } from '@/features/donors/types';
import type { ManagerActivityDto, ManagerActivityPeriod } from '../types';
import { dashboardKeys } from './dashboardKeys';

/** GET /api/v1/dashboard/manager-activity — donors contacted per user over the last 7 / 30 days. */
export function useManagerActivity(period: ManagerActivityPeriod) {
  return useQuery<ManagerActivityDto, ApiError>({
    queryKey: dashboardKeys.managerActivity(period),
    queryFn: async () => {
      const { data } = await api.get<{ data: ManagerActivityDto }>('/api/v1/dashboard/manager-activity', {
        params: { period },
      });
      return data.data;
    },
    staleTime: 60 * 1000,
  });
}
