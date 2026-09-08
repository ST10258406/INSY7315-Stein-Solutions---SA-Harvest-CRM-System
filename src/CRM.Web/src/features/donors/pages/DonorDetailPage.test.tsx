import { render, screen, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import DonorDetailPage from './DonorDetailPage';
import type { DonorDetailDto } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
  },
}));

function donorDetail(overrides: Partial<DonorDetailDto> = {}): DonorDetailDto {
  return {
    id: 'donor-1',
    status: 'Active',
    submissionSource: 'ManualCapture',
    foodspaceCompanyId: null,
    createdAt: '2026-01-01T09:00:00Z',
    updatedAt: '2026-02-01T09:00:00Z',
    company: {
      companyName: 'Acme Co',
      companyType: { id: 1, name: 'Retailer' },
      website: 'https://acme.example',
      registeredCompanyName: 'Acme Co (Pty) Ltd',
      tradingName: null,
      entityType: { id: 1, name: 'Private Company' },
      companyRegistrationNumber: '2020/123456/07',
      incomeTaxNumber: null,
    },
    primaryContact: { name: 'Jane Doe', jobTitle: 'Ops Manager', phone: '0821234567', email: 'jane@acme.example' },
    marketingContact: null,
    accountsContact: null,
    legalAddress: {
      streetNameNumber: '1 Main St',
      suburb: 'CBD',
      city: 'Cape Town',
      province: { id: 1, code: 'WC', name: 'Western Cape' },
      postalCode: '8001',
    },
    donations: {
      frequency: { id: 1, name: 'Weekly' },
      types: [{ id: 1, name: 'Fresh produce' }],
      collectionAddress: '1 Main St, Cape Town',
      operationsLogisticsDetails: 'Loading dock at the back.',
      operationalRegions: [{ id: 1, code: 'WC', name: 'Western Cape' }],
    },
    compliance: { bbbeeStatus: { id: 1, name: 'Level 2' }, documents: [] },
    crm: {
      relationshipManager: { id: 'rm-1', fullName: 'Sam RM' },
      marketingConsent: true,
      marketingConsentDate: '2026-01-05T00:00:00Z',
      impactReportingPreferences: 'Quarterly email summary',
      followUpDate: '2026-03-01',
      additionalInformation: 'Prefers morning calls.',
    },
    ...overrides,
  };
}

function renderPage(id = 'donor-1') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/donors/${id}`]}>
        <Routes>
          <Route path="/donors" element={<div data-testid="donors-list">Donors list</div>} />
          <Route path="/donors/:id" element={<DonorDetailPage />} />
          <Route path="/donors/:id/edit" element={<div data-testid="donor-edit">Edit page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('DonorDetailPage', () => {
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

  it('renders the Overview tab with real donor data by default', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail() } });

    renderPage();

    expect(await screen.findByRole('heading', { name: /Acme Co/ })).toBeInTheDocument();
    expect(screen.getByText('Acme Co (Pty) Ltd')).toBeInTheDocument();
    expect(screen.getByText('Weekly')).toBeInTheDocument();
  });

  it('switches to the Contacts tab and shows primary contact info, with a fallback for absent contacts', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail() } });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });

    await user.click(screen.getByRole('tab', { name: /Contacts/ }));

    expect(screen.getByText('Jane Doe')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'jane@acme.example' })).toHaveAttribute('href', 'mailto:jane@acme.example');
    expect(screen.getAllByText('Not provided.')).toHaveLength(2); // marketing + accounts contacts
  });

  it('switches to the Legal tab and shows address, compliance, and an empty documents state', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail() } });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });

    await user.click(screen.getByRole('tab', { name: 'Legal' }));

    expect(screen.getByText('1 Main St', { exact: false })).toBeInTheDocument();
    expect(screen.getByText('Level 2')).toBeInTheDocument();
    expect(screen.getByText('No documents uploaded for this donor.')).toBeInTheDocument();
  });

  it('lists existing documents on the Legal tab, with no upload/download/delete controls for a non-Admin viewer', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValueOnce({
      data: {
        data: donorDetail({
          compliance: {
            bbbeeStatus: null,
            documents: [
              {
                id: 'doc-1',
                documentType: 'BBBEECertificate',
                originalFileName: 'bbbee-cert.pdf',
                fileSizeBytes: 204800,
                mimeType: 'application/pdf',
                uploadedAt: '2026-01-10T00:00:00Z',
                isActive: true,
              },
            ],
          },
        }),
      },
    });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });
    await user.click(screen.getByRole('tab', { name: 'Legal' }));

    expect(screen.getByText('bbbee-cert.pdf')).toBeInTheDocument();
    // No authenticated Admin/SuperAdmin role in this test — upload dropzones,
    // the BBBEE download button, and delete are all RoleGuard-gated off.
    expect(screen.queryByText('Drop file here or browse')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /download/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /delete/i })).not.toBeInTheDocument();
  });

  it('shows upload dropzones and document download/delete controls on the Legal tab for an Admin viewer', async () => {
    const user = userEvent.setup();
    useAuthStore.setState({
      user: { id: 'admin-1', firstName: 'Ada', lastName: 'Min', email: 'ada@crm.local', roles: ['Admin'] },
      isAuthenticated: true,
    });
    vi.mocked(api.get).mockResolvedValueOnce({
      data: {
        data: donorDetail({
          compliance: {
            bbbeeStatus: null,
            documents: [
              {
                id: 'doc-1',
                documentType: 'BBBEECertificate',
                originalFileName: 'bbbee-cert.pdf',
                fileSizeBytes: 204800,
                mimeType: 'application/pdf',
                uploadedAt: '2026-01-10T00:00:00Z',
                isActive: true,
              },
            ],
          },
        }),
      },
    });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });
    await user.click(screen.getByRole('tab', { name: 'Legal' }));

    expect(screen.getByText('bbbee-cert.pdf')).toBeInTheDocument();
    expect(screen.getByText('BBBEE Certificate')).toBeInTheDocument();
    expect(screen.getByText('Signature')).toBeInTheDocument();
    expect(screen.getAllByText('Drop file here or browse')).toHaveLength(2);
    expect(screen.getByRole('button', { name: 'Download bbbee-cert.pdf' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete bbbee-cert.pdf' })).toBeInTheDocument();
  });

  it('switches to the CRM tab and shows RM, consent, and notes', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail() } });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });

    await user.click(screen.getByRole('tab', { name: 'CRM' }));

    // "Sam RM" now also appears in the header's relationship-manager badge.
    expect(screen.getAllByText('Sam RM').length).toBeGreaterThan(0);
    expect(screen.getByText('Given')).toBeInTheDocument();
    expect(screen.getByText('Prefers morning calls.')).toBeInTheDocument();
  });

  it('switches to the Activity tab and shows the real interaction timeline', async () => {
    const user = userEvent.setup();
    const emptyPage = { data: [], pagination: { page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } };
    vi.mocked(api.get).mockImplementation((url: string) => {
      if (url.endsWith('/interactions')) {
        return Promise.resolve({
          data: {
            data: [
              {
                id: 'int-1',
                donorId: 'donor-1',
                interactionType: 'Call',
                subject: 'Surplus volume forecast',
                body: 'Confirmed weekly chilled surplus through September.',
                emailAttachmentUrl: null,
                createdAt: '2026-02-20T09:00:00Z',
                createdBy: { id: 'user-1', fullName: 'Sam RM' },
              },
            ],
            pagination: { page: 1, pageSize: 20, totalCount: 1, totalPages: 1 },
          },
        });
      }
      if (url.endsWith('/tasks')) return Promise.resolve({ data: emptyPage });
      return Promise.resolve({ data: { data: donorDetail() } });
    });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });

    await user.click(screen.getByRole('tab', { name: /Activity/ }));

    expect(await screen.findByText('Interaction timeline')).toBeInTheDocument();
    expect(await screen.findByText('Surplus volume forecast')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /log interaction/i }).length).toBeGreaterThan(0);
  });

  it('the Edit button navigates to /donors/:id/edit', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail() } });

    renderPage();
    await screen.findByRole('heading', { name: /Acme Co/ });

    await user.click(screen.getByRole('link', { name: /edit/i }));

    expect(await screen.findByTestId('donor-edit')).toBeInTheDocument();
  });

  it('shows a clear not-found state for a 404, without crashing', async () => {
    vi.mocked(api.get).mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 404, data: { status: 404, code: 'NOT_FOUND', message: 'Donor not found.', errors: null } },
    });

    renderPage('missing-id');

    expect(await screen.findByText("This donor doesn't exist")).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole('link', { name: 'Back to donors' }));
    expect(await screen.findByTestId('donors-list')).toBeInTheDocument();
  });

  it('shows a generic error state with retry for a non-404 failure', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 500, data: { message: 'Server exploded' } },
    });
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail() } });

    renderPage();

    expect(await screen.findByText("Couldn't load this donor")).toBeInTheDocument();
    expect(screen.getByText('Server exploded')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => expect(api.get).toHaveBeenCalledTimes(2));
    expect(await screen.findByRole('heading', { name: /Acme Co/ })).toBeInTheDocument();
  });
});
