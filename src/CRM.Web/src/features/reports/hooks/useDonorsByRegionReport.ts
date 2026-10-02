import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { reportKeys } from './reportKeys';
import type { DonorsByRegion } from '../types';

/** All-time, no filter — every active region is always present (zero-filled by the backend). */
export function useDonorsByRegionReport() {
  return useQuery({
    queryKey: reportKeys.donorsByRegion(),
    queryFn: async () => {
      const { data } = await api.get<{ data: DonorsByRegion[] }>('/api/v1/reports/donors-by-region');
      return data.data;
    },
  });
}
