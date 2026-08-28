import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError, DonorFilters, DonorListItemDto, PaginatedResult } from '../types';

export function useDonors(filters: DonorFilters = {}) {
  return useQuery<PaginatedResult<DonorListItemDto>, ApiError>({
    queryKey: donorKeys.list(filters),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResult<DonorListItemDto>>('/api/v1/donors', {
        params: filters,
      });
      return data;
    },
  });
}
