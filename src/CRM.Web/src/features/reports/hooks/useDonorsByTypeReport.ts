import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { reportKeys } from './reportKeys';
import type { DonorsByType } from '../types';

/** All-time, no filter — every active donation type is always present (zero-filled by the backend). */
export function useDonorsByTypeReport() {
  return useQuery({
    queryKey: reportKeys.donorsByType(),
    queryFn: async () => {
      const { data } = await api.get<{ data: DonorsByType[] }>('/api/v1/reports/donors-by-type');
      return data.data;
    },
  });
}
