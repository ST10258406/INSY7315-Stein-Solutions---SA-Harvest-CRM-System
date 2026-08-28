import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError, CreateDonorRequest, DonorDetailDto } from '../types';

export function useCreateDonor() {
  const queryClient = useQueryClient();

  return useMutation<DonorDetailDto, ApiError, CreateDonorRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: DonorDetailDto }>('/api/v1/donors', request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: donorKeys.lists() });
    },
  });
}
