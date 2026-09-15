import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { reportKeys } from './reportKeys';
import type { DonorsByStatus } from '../types';

/** All-time, no filter — every status is always present (zero-filled by the backend), in a fixed display order. */
export function useDonorsByStatusReport() {
  return useQuery({
    queryKey: reportKeys.donorsByStatus(),
    queryFn: async () => {
      const { data } = await api.get<{ data: DonorsByStatus[] }>('/api/v1/reports/donors-by-status');
      return data.data;
    },
  });
}
