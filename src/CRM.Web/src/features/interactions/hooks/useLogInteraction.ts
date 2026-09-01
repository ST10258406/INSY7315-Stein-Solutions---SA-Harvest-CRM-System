import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { interactionKeys } from './interactionKeys';
import type { ApiError, InteractionLogDto, LogInteractionRequest } from '../types';

/**
 * POST /api/v1/donors/{id}/interactions. On success, invalidate this donor's
 * interaction timeline (so the feed refetches) plus the donor detail — a
 * `followUpDate` in the request updates `donors.follow_up_date` server-side.
 */
export function useLogInteraction(donorId: string) {
  const queryClient = useQueryClient();

  return useMutation<InteractionLogDto, ApiError, LogInteractionRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: InteractionLogDto }>(
        `/api/v1/donors/${donorId}/interactions`,
        request,
      );
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: interactionKeys.donor(donorId) });
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(donorId) });
    },
  });
}
