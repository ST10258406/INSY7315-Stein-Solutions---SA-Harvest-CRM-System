import { useInfiniteQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { interactionKeys } from './interactionKeys';
import type { ApiError, InteractionFilters, InteractionLogDto, PaginatedResult } from '../types';

const PAGE_SIZE = 20;

/**
 * Paginated donor interaction timeline (newest first). Uses an infinite query so
 * the Activity feed can append older pages with a "Load older" button rather than
 * flipping between pages. GET /api/v1/donors/{id}/interactions returns the
 * `PaginatedResult` envelope directly (no `{ data }` wrapper).
 */
export function useInteractions(
  donorId: string | undefined,
  filters: Omit<InteractionFilters, 'page' | 'pageSize'> = {},
) {
  return useInfiniteQuery<PaginatedResult<InteractionLogDto>, ApiError>({
    queryKey: interactionKeys.list(donorId, filters),
    enabled: !!donorId,
    initialPageParam: 1,
    queryFn: async ({ pageParam }) => {
      const { data } = await api.get<PaginatedResult<InteractionLogDto>>(
        `/api/v1/donors/${donorId}/interactions`,
        { params: { page: pageParam, pageSize: PAGE_SIZE, ...filters } },
      );
      return data;
    },
    getNextPageParam: (lastPage) => {
      const { page, totalPages } = lastPage.pagination;
      return page < totalPages ? page + 1 : undefined;
    },
  });
}
