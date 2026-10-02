import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError, DonorFilters, DonorListItemDto, PaginatedResult } from '../types';

/** GET /api/v1/donors — shared with callers that need the same cache entry via
 * `queryClient.fetchQuery` (e.g. the dashboard CSV export) rather than a hook. */
export async function fetchDonors(filters: DonorFilters): Promise<PaginatedResult<DonorListItemDto>> {
  const { data } = await api.get<PaginatedResult<DonorListItemDto>>('/api/v1/donors', {
    params: filters,
  });
  return data;
}

export function useDonors(filters: DonorFilters = {}) {
  return useQuery<PaginatedResult<DonorListItemDto>, ApiError>({
    queryKey: donorKeys.list(filters),
    queryFn: () => fetchDonors(filters),
    // Keep the current page's rows on screen while the next page/filter/sort
    // fetches, instead of flashing the table back to a loading skeleton.
    placeholderData: keepPreviousData,
  });
}
