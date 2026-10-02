import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError } from '@/features/donors/types';
import type { DashboardStatsDto } from '../types';
import { dashboardKeys } from './dashboardKeys';

// Unlike the notification bell (staleTime: 0, refetch on focus) these KPIs
// aren't time-critical — a minute of staleness on a task/follow-up count is fine.
const STALE_TIME = 60 * 1000;

export async function fetchDashboardStats(): Promise<DashboardStatsDto> {
  const { data } = await api.get<{ data: DashboardStatsDto }>('/api/v1/dashboard/stats');
  return data.data;
}

/** GET /api/v1/dashboard/stats — one call backs every Phase 1 + Phase 2 dashboard KPI card. */
export function useDashboardStats() {
  return useQuery<DashboardStatsDto, ApiError>({
    queryKey: dashboardKeys.stats(),
    queryFn: fetchDashboardStats,
    staleTime: STALE_TIME,
  });
}
