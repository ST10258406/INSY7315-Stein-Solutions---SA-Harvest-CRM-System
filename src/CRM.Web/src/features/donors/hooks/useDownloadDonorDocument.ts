import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError, DocumentDownloadUrlDto } from '../types';

interface DownloadDonorDocumentVariables {
  documentId: string;
}

/**
 * Fetches a fresh SAS URL and triggers the browser download immediately.
 * The URL expires in 15 minutes and is single-use by design (see
 * GetDocumentDownloadUrlQueryHandler) — it is deliberately never returned
 * from this hook, cached, or held in component state. Fire-and-forget only.
 */
export function useDownloadDonorDocument(donorId: string) {
  return useMutation<void, ApiError, DownloadDonorDocumentVariables>({
    mutationFn: async ({ documentId }) => {
      const { data } = await api.get<{ data: DocumentDownloadUrlDto }>(
        `/api/v1/donors/${donorId}/documents/${documentId}/download`
      );

      const link = document.createElement('a');
      link.href = data.data.downloadUrl;
      link.download = data.data.originalFileName;
      link.rel = 'noopener';
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      link.remove();
    },
  });
}
