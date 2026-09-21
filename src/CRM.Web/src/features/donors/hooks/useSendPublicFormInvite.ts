import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/axios';
import type { ApiError, SendPublicFormInviteRequest, SendPublicFormInviteResponseDto } from '../types';

/**
 * POST /api/v1/donors/public-form-invite. Unlike donor mutations, there is no
 * donor record yet to invalidate — this just emails the public onboarding
 * form link to a prospective contact. A failed send is a real error (the
 * backend rejects it outright, EMAIL_DELIVERY_FAILED), so the user must be
 * told rather than assuming it went out.
 */
export function useSendPublicFormInvite() {
  return useMutation<SendPublicFormInviteResponseDto, ApiError, SendPublicFormInviteRequest>({
    mutationFn: async (request) => {
      const { data } = await api.post<{ data: SendPublicFormInviteResponseDto }>(
        '/api/v1/donors/public-form-invite',
        request,
      );
      return data.data;
    },
    onSuccess: () => {
      toast.success('Invitation sent.');
    },
    onError: (error) => {
      toast.error(error.response?.data?.message ?? 'Could not send this invitation. Please try again.');
    },
  });
}
