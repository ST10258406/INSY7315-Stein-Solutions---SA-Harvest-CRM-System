import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError } from '@/features/donors/types';
import { vettingKeys } from './vettingKeys';
import type { CreateOfficerRequest, UpdateOfficerRequest, VettingOfficer } from '../types';

/** GET /api/v1/vetting/officers — every officer, by name. Not paged: it fills dropdowns. */
export function useOfficers() {
  return useQuery<VettingOfficer[], ApiError>({
    queryKey: vettingKeys.officers(),
    queryFn: async () => {
      const { data } = await api.get<{ data: VettingOfficer[] }>('/api/v1/vetting/officers');
      return data.data;
    },
  });
}

/** POST /api/v1/vetting/officers. Admin only. */
export function useCreateOfficer() {
  const queryClient = useQueryClient();
  return useMutation<VettingOfficer, ApiError, CreateOfficerRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: VettingOfficer }>('/api/v1/vetting/officers', request);
      return data.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: vettingKeys.officers() }),
  });
}

/** PATCH /api/v1/vetting/officers/{id}. Admin only. workloadCount is server-calculated and never sent. */
export function useUpdateOfficer() {
  const queryClient = useQueryClient();
  return useMutation<VettingOfficer, ApiError, { id: string; request: UpdateOfficerRequest }>({
    mutationFn: async ({ id, request }) => {
      const { data } = await api.patch<{ data: VettingOfficer }>(`/api/v1/vetting/officers/${id}`, request);
      return data.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: vettingKeys.officers() }),
  });
}
