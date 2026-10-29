import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError } from '@/features/donors/types';
import { vettingKeys } from './vettingKeys';
import type { CreateSiteVisitRequest, UpdateSiteVisitRequest, VettingSiteVisit } from '../types';

/** GET /api/v1/vetting/site-visits?date=&officerId= — one day's visits, in scheduled-time order. */
export function useSiteVisits(date: string, officerId?: string) {
  return useQuery<VettingSiteVisit[], ApiError>({
    queryKey: vettingKeys.siteVisitDay(date, officerId),
    queryFn: async () => {
      const { data } = await api.get<{ data: VettingSiteVisit[] }>('/api/v1/vetting/site-visits', {
        params: { date, officerId: officerId || undefined },
      });
      return data.data;
    },
    placeholderData: keepPreviousData,
  });
}

// Visits feed each officer's workload count, so every visit change refreshes the officers too.
function useInvalidateVisits() {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: vettingKeys.siteVisits() });
    queryClient.invalidateQueries({ queryKey: vettingKeys.officers() });
  };
}

/** POST /api/v1/vetting/site-visits. Status starts as ToDo. */
export function useCreateSiteVisit() {
  const invalidate = useInvalidateVisits();
  return useMutation<VettingSiteVisit, ApiError, CreateSiteVisitRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: VettingSiteVisit }>('/api/v1/vetting/site-visits', request);
      return data.data;
    },
    onSuccess: invalidate,
  });
}

/** PATCH /api/v1/vetting/site-visits/{id}. `id` is a mutation argument because one hook serves every row. */
export function useUpdateSiteVisit() {
  const invalidate = useInvalidateVisits();
  return useMutation<VettingSiteVisit, ApiError, { id: string; request: UpdateSiteVisitRequest }>({
    mutationFn: async ({ id, request }) => {
      const { data } = await api.patch<{ data: VettingSiteVisit }>(`/api/v1/vetting/site-visits/${id}`, request);
      return data.data;
    },
    onSuccess: invalidate,
  });
}
