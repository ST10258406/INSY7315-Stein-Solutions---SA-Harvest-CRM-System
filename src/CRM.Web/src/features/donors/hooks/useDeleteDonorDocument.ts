import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { donorKeys } from './donorKeys';
import type { ApiError } from '../types';

interface DeleteDonorDocumentVariables {
  documentId: string;
}

export function useDeleteDonorDocument(donorId: string) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, DeleteDonorDocumentVariables>({
    mutationFn: async ({ documentId }) => {
      await api.delete(`/api/v1/donors/${donorId}/documents/${documentId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: donorKeys.detail(donorId) });
    },
  });
}
