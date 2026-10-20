import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError, CreateSavedFilterRequest, SavedFilterDto } from './types';

const URL = '/api/v1/saved-filters';

export const savedFilterKeys = {
  all: ['savedFilters'] as const,
};

/** GET the caller's saved donor filters. `enabled` lets the rail popover fetch only while open. */
export function useSavedFilters(enabled = true) {
  return useQuery<SavedFilterDto[], ApiError>({
    queryKey: savedFilterKeys.all,
    queryFn: async () => (await api.get<SavedFilterDto[]>(URL, { params: { page: 'Donors' } })).data,
    enabled,
  });
}

export function useCreateSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation<SavedFilterDto, ApiError, CreateSavedFilterRequest>({
    mutationFn: async (request) => (await api.post<{ data: SavedFilterDto }>(URL, request)).data.data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: savedFilterKeys.all }),
  });
}

export function useDeleteSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: async (id) => {
      await api.delete(`${URL}/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: savedFilterKeys.all }),
  });
}
