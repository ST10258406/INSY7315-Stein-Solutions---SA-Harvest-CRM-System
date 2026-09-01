import { render, screen, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { DocumentList } from './DocumentList';
import type { DonorDocumentDto } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
    delete: vi.fn(),
  },
}));

const bbbeeDoc: DonorDocumentDto = {
  id: 'doc-1',
  documentType: 'BBBEECertificate',
  originalFileName: 'bbbee-cert.pdf',
  fileSizeBytes: 204800,
  mimeType: 'application/pdf',
  uploadedAt: '2026-01-10T00:00:00Z',
  isActive: true,
};

const signatureDoc: DonorDocumentDto = {
  id: 'doc-2',
  documentType: 'Signature',
  originalFileName: 'sig.png',
  fileSizeBytes: 2048,
  mimeType: 'image/png',
  uploadedAt: '2026-01-11T00:00:00Z',
  isActive: true,
};

function renderList(documents: DonorDocumentDto[]) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DocumentList donorId="donor-1" documents={documents} />
    </QueryClientProvider>
  );
}

describe('DocumentList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isHydrating: false,
    });
  });
  afterEach(cleanup);

  it('shows the empty state when there are no documents', () => {
    renderList([]);
    expect(screen.getByText('No documents uploaded for this donor.')).toBeInTheDocument();
  });

  it('an unrestricted document type (Signature) shows its download button to any viewer', () => {
    renderList([signatureDoc]);
    expect(screen.getByRole('button', { name: 'Download sig.png' })).toBeInTheDocument();
  });

  it('hides the BBBEE download and delete controls for a non-Admin viewer', () => {
    renderList([bbbeeDoc]);
    expect(screen.queryByRole('button', { name: 'Download bbbee-cert.pdf' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete bbbee-cert.pdf' })).not.toBeInTheDocument();
  });

  it('shows the BBBEE download and delete controls for an Admin viewer', () => {
    useAuthStore.setState({
      user: { id: '1', firstName: 'Ada', lastName: 'Min', email: 'ada@crm.local', roles: ['Admin'] },
      isAuthenticated: true,
    });
    renderList([bbbeeDoc]);
    expect(screen.getByRole('button', { name: 'Download bbbee-cert.pdf' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete bbbee-cert.pdf' })).toBeInTheDocument();
  });

  it('clicking download fetches a fresh SAS URL for that document', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValueOnce({
      data: { data: { downloadUrl: 'https://blob.example/sig.png?sig=x', expiresAt: '2026-01-11T00:15:00Z', originalFileName: 'sig.png' } },
    });
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});

    renderList([signatureDoc]);
    await user.click(screen.getByRole('button', { name: 'Download sig.png' }));

    await waitFor(() => expect(api.get).toHaveBeenCalledWith('/api/v1/donors/donor-1/documents/doc-2/download'));
    expect(clickSpy).toHaveBeenCalled();
    clickSpy.mockRestore();
  });

  it('delete requires confirmation before firing the DELETE request', async () => {
    const user = userEvent.setup();
    useAuthStore.setState({
      user: { id: '1', firstName: 'Ada', lastName: 'Min', email: 'ada@crm.local', roles: ['SuperAdmin'] },
      isAuthenticated: true,
    });
    vi.mocked(api.delete).mockResolvedValueOnce({ data: undefined });

    renderList([bbbeeDoc]);
    await user.click(screen.getByRole('button', { name: 'Delete bbbee-cert.pdf' }));

    expect(screen.getByText("Delete this document? This can't be undone.")).toBeInTheDocument();
    expect(api.delete).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Delete' }));

    await waitFor(() => expect(api.delete).toHaveBeenCalledWith('/api/v1/donors/donor-1/documents/doc-1'));
  });

  it('cancelling the confirmation does not fire the DELETE request', async () => {
    const user = userEvent.setup();
    useAuthStore.setState({
      user: { id: '1', firstName: 'Ada', lastName: 'Min', email: 'ada@crm.local', roles: ['Admin'] },
      isAuthenticated: true,
    });

    renderList([bbbeeDoc]);
    await user.click(screen.getByRole('button', { name: 'Delete bbbee-cert.pdf' }));
    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(screen.queryByText("Delete this document? This can't be undone.")).not.toBeInTheDocument();
    expect(api.delete).not.toHaveBeenCalled();
  });
});
