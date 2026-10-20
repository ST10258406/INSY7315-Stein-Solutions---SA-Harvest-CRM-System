import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError } from '@/features/donors/types';
import { vettingKeys } from './vettingKeys';
import type { VettingDownloadUrl, VettingFileSlot, VettingFiles, VettingPhoto, VettingSubmission } from '../types';

// SAS URLs expire after 15 minutes (server-side ceiling); refetch thumbnails well before that.
const THUMBNAIL_STALE_MS = 10 * 60 * 1000;

/** Either an evidence photo slot or an assessment file slot. */
export type VettingUploadTarget = { shotIndex: number } | { slot: VettingFileSlot };

const targetPath = (submissionId: string, target: VettingUploadTarget) =>
  'shotIndex' in target
    ? `/api/v1/vetting/submissions/${submissionId}/photos/${target.shotIndex}`
    : `/api/v1/vetting/submissions/${submissionId}/files/${target.slot}`;

async function fetchDownloadUrl(submissionId: string, target: VettingUploadTarget) {
  const { data } = await api.get<{ data: VettingDownloadUrl }>(`${targetPath(submissionId, target)}/download`);
  return data.data;
}

/** POST a photo or file (multipart `file`). Allowed only while New or Returned (409 otherwise). */
export function useUploadVettingFile(submissionId: string, target: VettingUploadTarget) {
  const queryClient = useQueryClient();
  return useMutation<VettingPhoto | VettingFiles, ApiError, File>({
    mutationFn: async (file) => {
      const formData = new FormData();
      formData.append('file', file);
      const { data } = await api.post<{ data: VettingPhoto | VettingFiles }>(targetPath(submissionId, target), formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return data.data;
    },
    // The photo/file state lives on the submission payload.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: vettingKeys.submission(submissionId) }),
  });
}

/**
 * Fetches a fresh SAS URL and opens it in a new tab, like useDownloadDonorDocument. The URL is
 * never cached or kept in state.
 */
export function useVettingFileDownload(submissionId: string) {
  return useMutation<void, ApiError, VettingUploadTarget>({
    mutationFn: async (target) => {
      const { downloadUrl, originalFileName } = await fetchDownloadUrl(submissionId, target);
      // An anchor click, not window.open: Safari blocks window.open after an await.
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.download = originalFileName;
      link.rel = 'noopener';
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      link.remove();
    },
  });
}

/** Thumbnail URL for a captured photo. Pass enabled=false until its section is opened. */
export function useVettingPhotoUrl(submissionId: string, photo: VettingPhoto, enabled: boolean) {
  return useQuery<string, ApiError>({
    queryKey: vettingKeys.photoUrl(submissionId, photo.shotIndex, photo.capturedAt),
    queryFn: async () => (await fetchDownloadUrl(submissionId, { shotIndex: photo.shotIndex })).downloadUrl,
    enabled: enabled && photo.isCaptured,
    staleTime: THUMBNAIL_STALE_MS,
  });
}

/** POST /{id}/submit — New/Returned → Ready. 400 when photos are missing, 409 in any other state. */
export function useSubmitVetting(submissionId: string) {
  const queryClient = useQueryClient();
  return useMutation<VettingSubmission, ApiError, void>({
    mutationFn: async () => {
      const { data } = await api.post<{ data: VettingSubmission }>(`/api/v1/vetting/submissions/${submissionId}/submit`);
      return data.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vettingKeys.submissions() });
      queryClient.invalidateQueries({ queryKey: vettingKeys.siteVisits() });
      queryClient.invalidateQueries({ queryKey: vettingKeys.officers() });
    },
  });
}
