import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError, PaginatedResult } from '@/features/donors/types';
import { vettingKeys } from './vettingKeys';
import type {
  CreatedSubmission,
  CreateSubmissionRequest,
  UpdateSubmissionRequest,
  VettingSubmission,
  VettingSubmissionFilters,
  VettingSubmissionListItem,
} from '../types';

/** GET /api/v1/vetting/submissions — the PaginatedResult envelope, newest first. */
export function useVettingSubmissions(filters: VettingSubmissionFilters = {}, { enabled = true } = {}) {
  return useQuery<PaginatedResult<VettingSubmissionListItem>, ApiError>({
    enabled,
    queryKey: vettingKeys.submissionList(filters),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResult<VettingSubmissionListItem>>('/api/v1/vetting/submissions', { params: filters });
      return data;
    },
    placeholderData: keepPreviousData,
  });
}

/** GET /api/v1/vetting/submissions/{id} — submission with details, active flags, photos and filled file slots. */
export function useVetting(id: string | undefined) {
  return useQuery<VettingSubmission, ApiError>({
    queryKey: vettingKeys.submission(id),
    queryFn: async () => {
      const { data } = await api.get<{ data: VettingSubmission }>(`/api/v1/vetting/submissions/${id}`);
      return data.data;
    },
    enabled: !!id,
  });
}

/** POST /api/v1/vetting/submissions. Also starts the linked visit and changes the officer's workload. */
export function useCreateVetting() {
  const queryClient = useQueryClient();
  return useMutation<CreatedSubmission, ApiError, CreateSubmissionRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: CreatedSubmission }>('/api/v1/vetting/submissions', request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vettingKeys.submissions() });
      queryClient.invalidateQueries({ queryKey: vettingKeys.siteVisits() });
      queryClient.invalidateQueries({ queryKey: vettingKeys.officers() });
    },
  });
}

/** PATCH /api/v1/vetting/submissions/{id} — replaces details; only while New or Returned (409 otherwise). */
export function useUpdateVetting(id: string) {
  const queryClient = useQueryClient();
  return useMutation<VettingSubmission, ApiError, UpdateSubmissionRequest>({
    mutationFn: async (request) => {
      const { data } = await api.patch<{ data: VettingSubmission }>(`/api/v1/vetting/submissions/${id}`, request);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vettingKeys.submissions() });
      queryClient.invalidateQueries({ queryKey: vettingKeys.officers() });
    },
  });
}
