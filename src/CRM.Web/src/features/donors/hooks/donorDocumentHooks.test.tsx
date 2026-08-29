import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { useUploadDonorDocument } from './useUploadDonorDocument';
import { useDeleteDonorDocument } from './useDeleteDonorDocument';
import { useDownloadDonorDocument } from './useDownloadDonorDocument';
import { donorKeys } from './donorKeys';
import type { DonorDocumentDto, DocumentDownloadUrlDto } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}));

const sampleDocument: DonorDocumentDto = {
  id: 'doc-1',
  documentType: 'BBBEECertificate',
  originalFileName: 'bbbee-cert.pdf',
  fileSizeBytes: 204800,
  mimeType: 'application/pdf',
  uploadedAt: '2026-01-10T00:00:00Z',
  isActive: true,
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
  };
}

describe('donor document hooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('useUploadDonorDocument → POSTs multipart form data to /donors/{id}/documents and invalidates the donor detail cache', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: sampleDocument } });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useUploadDonorDocument('donor-1'), { wrapper });

    const file = new File(['%PDF-1.4'], 'bbbee-cert.pdf', { type: 'application/pdf' });
    result.current.mutate({ documentType: 'BBBEECertificate', file });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith(
      '/api/v1/donors/donor-1/documents',
      expect.any(FormData),
      expect.objectContaining({ headers: expect.objectContaining({ 'Content-Type': 'multipart/form-data' }) })
    );
    const formData = vi.mocked(api.post).mock.calls[0][1] as FormData;
    expect(formData.get('documentType')).toBe('BBBEECertificate');
    expect(formData.get('file')).toBeInstanceOf(File);

    expect(result.current.data).toEqual(sampleDocument);
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.detail('donor-1') });
  });

  it('useUploadDonorDocument → surfaces the API error envelope on failure', async () => {
    vi.mocked(api.post).mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 400, data: { message: 'Unsupported file type for this document type.' } },
    });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useUploadDonorDocument('donor-1'), { wrapper });

    result.current.mutate({
      documentType: 'Signature',
      file: new File(['bytes'], 'sig.png', { type: 'image/png' }),
    });

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error?.response?.data?.message).toBe('Unsupported file type for this document type.');
  });

  it('useDeleteDonorDocument → DELETEs /donors/{id}/documents/{docId} and invalidates the donor detail cache', async () => {
    vi.mocked(api.delete).mockResolvedValueOnce({ data: undefined });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useDeleteDonorDocument('donor-1'), { wrapper });

    result.current.mutate({ documentId: 'doc-1' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.delete).toHaveBeenCalledWith('/api/v1/donors/donor-1/documents/doc-1');
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.detail('donor-1') });
  });

  it('useDownloadDonorDocument → GETs the SAS URL and immediately triggers an anchor-click download, never exposing the URL from the hook', async () => {
    const downloadUrl: DocumentDownloadUrlDto = {
      downloadUrl: 'https://blob.example/donors/donor-1/bbbee-cert.pdf?sig=abc',
      expiresAt: '2026-01-10T00:15:00Z',
      originalFileName: 'bbbee-cert.pdf',
    };
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: downloadUrl } });

    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    const appendSpy = vi.spyOn(document.body, 'appendChild');

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useDownloadDonorDocument('donor-1'), { wrapper });

    result.current.mutate({ documentId: 'doc-1' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.get).toHaveBeenCalledWith('/api/v1/donors/donor-1/documents/doc-1/download');
    // renderHook mounts its own container into document.body first, so the
    // anchor isn't necessarily the first appendChild call — find it by tag.
    const anchorCall = appendSpy.mock.calls.find(([node]) => (node as HTMLElement).tagName === 'A');
    const anchor = anchorCall?.[0] as HTMLAnchorElement;
    expect(anchor).toBeDefined();
    expect(anchor.href).toBe(downloadUrl.downloadUrl);
    expect(anchor.download).toBe('bbbee-cert.pdf');
    expect(clickSpy).toHaveBeenCalledTimes(1);
    // The mutation resolves to void — the caller never receives the URL back,
    // so it can't be cached, stored, or reused (see Issue 44).
    expect(result.current.data).toBeUndefined();

    clickSpy.mockRestore();
    appendSpy.mockRestore();
  });
});
