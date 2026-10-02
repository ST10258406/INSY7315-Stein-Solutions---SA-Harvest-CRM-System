import { useMutation } from '@tanstack/react-query';
import { publicApi } from '@/lib/publicApi';
import type { ApiError } from '@/features/donors/types';
import type { SubmitPublicDonorRequest } from '../lib/publicDonorMapping';

export interface SubmitPublicDonorResponse {
  message: string;
  referenceNumber: string;
  submissionToken: string;
}

export function useSubmitPublicDonor() {
  return useMutation<SubmitPublicDonorResponse, ApiError, SubmitPublicDonorRequest>({
    mutationFn: async (request) => {
      const { data } = await publicApi.post<SubmitPublicDonorResponse>('/api/v1/public/donors/submit', request);
      return data;
    },
  });
}
