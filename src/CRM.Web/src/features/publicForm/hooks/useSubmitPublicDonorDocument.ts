import { useMutation } from '@tanstack/react-query';
import { publicApi } from '@/lib/publicApi';
import type { ApiError } from '@/features/donors/types';

interface SubmitPublicDonorDocumentVariables {
  sessionToken: string;
  file: File;
}

interface SubmitPublicDonorDocumentResponse {
  message: string;
}

// Matches the backend's exact multipart field names for this endpoint
// (sessionToken, documentType, file) — see SubmitPublicDonorDocumentCommand.
// Deliberately separate from useUploadDonorDocument (the authenticated
// /donors/:id/documents flow): different base client (no auth header, no
// 401-refresh interceptor), different field set, different response shape
// (no `data` wrapper, just `{ message }`).
export function useSubmitPublicDonorDocument() {
  return useMutation<SubmitPublicDonorDocumentResponse, ApiError, SubmitPublicDonorDocumentVariables>({
    mutationFn: async ({ sessionToken, file }) => {
      const formData = new FormData();
      formData.append('sessionToken', sessionToken);
      formData.append('documentType', 'BBBEECertificate');
      formData.append('file', file);

      const { data } = await publicApi.post<SubmitPublicDonorDocumentResponse>(
        '/api/v1/public/donors/submit/document',
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      );
      return data;
    },
  });
}
