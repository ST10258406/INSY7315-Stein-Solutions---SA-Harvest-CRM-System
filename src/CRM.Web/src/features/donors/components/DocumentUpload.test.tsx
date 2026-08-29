import { render, screen, waitFor, cleanup, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { DocumentUpload } from './DocumentUpload';

vi.mock('@/lib/axios', () => ({
  api: {
    post: vi.fn(),
  },
}));

function renderUpload(documentType: 'BBBEECertificate' | 'Signature' = 'BBBEECertificate') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DocumentUpload donorId="donor-1" documentType={documentType} />
    </QueryClientProvider>
  );
}

describe('DocumentUpload', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  it('rejects an oversized file client-side without calling the API', async () => {
    const user = userEvent.setup();
    renderUpload('Signature');

    const oversized = new File([new Uint8Array(6 * 1024 * 1024)], 'sig.png', { type: 'image/png' });
    const input = screen.getByLabelText('Signature file input');
    await user.upload(input, oversized);

    expect(await screen.findByText(/exceeds the 5.0 MB limit/i)).toBeInTheDocument();
    expect(api.post).not.toHaveBeenCalled();
  });

  it('rejects an unsupported mime type client-side without calling the API', async () => {
    renderUpload('Signature');

    // fireEvent.change (not userEvent.upload) deliberately bypasses the
    // input's `accept` filtering — this is the drag-and-drop path, which
    // never respects `accept`, so the component's own validation is the
    // only thing standing between a wrong-type file and the API.
    const wrongType = new File(['not a png'], 'sig.jpg', { type: 'image/jpeg' });
    const input = screen.getByLabelText('Signature file input');
    fireEvent.change(input, { target: { files: [wrongType] } });

    expect(await screen.findByText(/Unsupported file type for Signature/i)).toBeInTheDocument();
    expect(api.post).not.toHaveBeenCalled();
  });

  it('uploads a valid file and shows a success confirmation', async () => {
    const user = userEvent.setup();
    vi.mocked(api.post).mockResolvedValueOnce({
      data: {
        data: {
          id: 'doc-1',
          documentType: 'BBBEECertificate',
          originalFileName: 'cert.pdf',
          fileSizeBytes: 1024,
          mimeType: 'application/pdf',
          uploadedAt: '2026-01-10T00:00:00Z',
          isActive: true,
        },
      },
    });
    renderUpload('BBBEECertificate');

    const validFile = new File(['%PDF-1.4'], 'cert.pdf', { type: 'application/pdf' });
    const input = screen.getByLabelText('BBBEE Certificate file input');
    await user.upload(input, validFile);

    await waitFor(() => expect(api.post).toHaveBeenCalledTimes(1));
    expect(await screen.findByText('Uploaded.')).toBeInTheDocument();
  });

  it('surfaces a server-side rejection (e.g. backend-only mime/size check) as an error message', async () => {
    const user = userEvent.setup();
    vi.mocked(api.post).mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 400, data: { message: 'File exceeds 5MB limit.' } },
    });
    renderUpload('BBBEECertificate');

    const validFile = new File(['%PDF-1.4'], 'cert.pdf', { type: 'application/pdf' });
    const input = screen.getByLabelText('BBBEE Certificate file input');
    await user.upload(input, validFile);

    expect(await screen.findByText('File exceeds 5MB limit.')).toBeInTheDocument();
  });
});
