import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError } from '@/features/donors/types';
import { vettingKeys } from './vettingKeys';
import type { DecideSubmissionRequest, VettingFlag, VettingSubmission } from '../types';

/**
 * POST /{id}/decision (Admin). Invalidates every submission query (detail and the review queue)
 * and the officers (workload changes), including on a 409, when another admin got there first.
 */
export function useVettingDecision(submissionId: string) {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: vettingKeys.submissions() });
    queryClient.invalidateQueries({ queryKey: vettingKeys.officers() });
  };
  return useMutation<VettingSubmission, ApiError, DecideSubmissionRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: VettingSubmission }>(`/api/v1/vetting/submissions/${submissionId}/decision`, request);
      return data.data;
    },
    onSuccess: invalidate,
    onError: (error) => {
      if (error.response?.status === 409) invalidate();
    },
  });
}

/** POST /{id}/flags (Admin, Ready only) — a Reviewer flag, which survives resubmits. */
export function useAddReviewerFlag(submissionId: string) {
  const queryClient = useQueryClient();
  return useMutation<VettingFlag, ApiError, string>({
    mutationFn: async (description) => {
      const { data } = await api.post<{ data: VettingFlag }>(`/api/v1/vetting/submissions/${submissionId}/flags`, { description });
      return data.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: vettingKeys.submissions() }),
  });
}
