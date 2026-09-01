import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError, DocumentTypeCode, DonorDocumentDto } from '../types';

interface UploadDonorDocumentVariables {
  documentType: DocumentTypeCode;
  file: File;
}

export function useUploadDonorDocument(donorId: string) {
  const queryClient = useQueryClient();

  return useMutation<DonorDocumentDto, ApiError, UploadDonorDocumentVariables>({
    mutationFn: async ({ documentType, file }) => {
      const formData = new FormData();
      formData.append('documentType', documentType);
      formData.append('file', file);

      // Axios strips any Content-Type we set here once it sees a FormData
      // body and lets the browser generate the multipart boundary itself —
      // this header is just documentation of intent.
      const { data } = await api.post<{ data: DonorDocumentDto }>(
        `/api/v1/donors/${donorId}/documents`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      );
      return data.data;
    },
    onSuccess: () => {
      // The document list lives on the donor detail payload, not its own
      // endpoint — refetching it is what refreshes the list without a full
      // page reload.
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(donorId) });
    },
  });
}
