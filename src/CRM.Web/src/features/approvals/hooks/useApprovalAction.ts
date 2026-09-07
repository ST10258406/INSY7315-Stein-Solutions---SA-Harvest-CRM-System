import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { approvalKeys } from './approvalKeys';
import type { ApiError, ApprovalActionInput } from '../types';

/**
 * One mutation for both approval decisions:
 *  - POST /api/v1/approvals/{id}/approve       (204)
 *  - POST /api/v1/approvals/{id}/reject {reason} (204)
 *
 * Both move the donor's status server-side (Active / Rejected) and fire a
 * notification, so on success we invalidate the whole approval queue AND every
 * donor list/detail cache.
 */
export function useApprovalAction() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, ApprovalActionInput>({
    mutationFn: async (input) => {
      if (input.action === 'approve') {
        await api.post(`/api/v1/approvals/${input.approvalId}/approve`);
      } else {
        await api.post(`/api/v1/approvals/${input.approvalId}/reject`, {
          rejectionReason: input.rejectionReason,
        });
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: approvalKeys.all });
      queryClient.invalidateQueries({ queryKey: donorKeys.all });
    },
  });
}
