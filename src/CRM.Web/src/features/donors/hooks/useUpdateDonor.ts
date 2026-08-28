import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError, DonorDetailDto, UpdateDonorRequest } from '../types';

export function useUpdateDonor(id: string) {
  const queryClient = useQueryClient();

  return useMutation<DonorDetailDto, ApiError, UpdateDonorRequest>({
    mutationFn: async (request) => {
      const { data } = await api.patch<{ data: DonorDetailDto }>(`/api/v1/donors/${id}`, request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: donorKeys.lists() });
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(id) });
    },
  });
}
