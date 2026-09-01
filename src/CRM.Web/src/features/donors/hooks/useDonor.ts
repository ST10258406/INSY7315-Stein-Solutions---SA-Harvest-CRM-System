import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError, DonorDetailDto } from '../types';

export function useDonor(id: string | undefined) {
  return useQuery<DonorDetailDto, ApiError>({
    queryKey: donorKeys.detail(id),
    queryFn: async () => {
      const { data } = await api.get<{ data: DonorDetailDto }>(`/api/v1/donors/${id}`);
      return data.data;
    },
    enabled: !!id,
  });
}
