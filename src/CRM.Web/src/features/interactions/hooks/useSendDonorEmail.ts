import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { interactionKeys } from './interactionKeys';
import type { ApiError, InteractionLogDto, SendDonorEmailRequest } from '../types';

/**
 * POST /api/v1/donors/{id}/interactions/email. Unlike useLogInteraction, a failed
 * send is a real error here (not a manual note) — the backend rejects it outright
 * (EMAIL_DELIVERY_FAILED) rather than silently logging a Failed EmailLog, so the
 * user must be told: it won't appear in the timeline either. On success, invalidate
 * the interaction timeline so the new Email interaction appears without a refresh.
 */
export function useSendDonorEmail(donorId: string) {
  const queryClient = useQueryClient();

  return useMutation<InteractionLogDto, ApiError, SendDonorEmailRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: InteractionLogDto }>(
        `/api/v1/donors/${donorId}/interactions/email`,
        request,
      );
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: interactionKeys.donor(donorId) });
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(donorId) });
      toast.success('Email sent.');
    },
    onError: (error) => {
      toast.error(error.response?.data?.message ?? 'Could not send this email. Please try again.');
    },
  });
}
