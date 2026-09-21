import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { ApiError, PaginatedResult, UserFilters, UserListItemDto } from '../types';

export function useUsers(filters: UserFilters = {}) {
  return useQuery<PaginatedResult<UserListItemDto>, ApiError>({
    queryKey: userKeys.list(filters),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResult<UserListItemDto>>('/api/v1/users', {
        params: filters,
      });
      return data;
    },
    // Keep the current page's rows on screen while the next page/filter/sort
    // fetches, instead of flashing the table back to a loading skeleton.
    placeholderData: keepPreviousData,
  });
}
