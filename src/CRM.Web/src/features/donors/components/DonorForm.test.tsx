import { render, screen, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { api } from '@/lib/axios';
import { DonorForm } from './DonorForm';
import type { DonorDetailDto } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}));

const LOOKUPS: Record<string, unknown[]> = {
  '/api/v1/lookups/company-types': [{ id: 1, name: 'Retailer' }],
  '/api/v1/lookups/entity-types': [{ id: 1, name: 'Private Company' }],
  '/api/v1/lookups/operational-regions': [{ id: 1, code: 'WC', name: 'Western Cape' }],
  '/api/v1/lookups/donation-types': [{ id: 1, name: 'Fresh produce' }],
  '/api/v1/lookups/donation-frequencies': [{ id: 1, name: 'Weekly' }],
  '/api/v1/lookups/provinces': [{ id: 1, code: 'WC', name: 'Western Cape' }],
  '/api/v1/lookups/bbbee-statuses': [{ id: 1, name: 'Level 2' }],
};

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
      incomeTaxNumber: '9123456789',
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
      relationshipManager: null,
      marketingConsent: true,
      marketingConsentDate: '2026-01-05T00:00:00Z',
      impactReportingPreferences: 'Quarterly email summary',
      followUpDate: null,
      additionalInformation: 'Prefers morning calls.',
    },
    ...overrides,
  };
}

function mockApi({ donor }: { donor?: DonorDetailDto } = {}) {
  vi.mocked(api.get).mockImplementation((url: string) => {
    if (url in LOOKUPS) return Promise.resolve({ data: { data: LOOKUPS[url] } });
    if (donor && url === `/api/v1/donors/${donor.id}`) return Promise.resolve({ data: { data: donor } });
    return Promise.resolve({ data: { data: [] } });
  });
}

function renderForm(mode: 'create' | 'edit', donorId?: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const initialEntry = mode === 'create' ? '/donors/new' : `/donors/${donorId}/edit`;

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/donors/new" element={<DonorForm mode="create" />} />
          <Route path="/donors/:id/edit" element={<DonorForm mode="edit" donorId={donorId} />} />
          <Route path="/donors/:id" element={<div data-testid="donor-detail">Detail page</div>} />
          <Route path="/donors" element={<div data-testid="donors-list">Donors list</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

// Fills every required field for a valid submission, leaving toggled-off
// optional sections (marketing/accounts contact) untouched.
async function fillRequiredFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Company name'), 'Acme Co');
  await user.selectOptions(screen.getByLabelText('Company type'), '1');
  await user.type(screen.getByLabelText('Website'), 'https://acme.example');
  await user.type(screen.getByLabelText('Registered company name'), 'Acme Co (Pty) Ltd');
  await user.type(screen.getByLabelText('Trading name'), 'Acme');
  await user.selectOptions(screen.getByLabelText('Entity type'), '1');
  await user.type(screen.getByLabelText('Company registration number'), '2020/123456/07');
  await user.type(screen.getByLabelText('Income tax number'), '9123456789');

  await user.type(screen.getByLabelText('Name'), 'Jane Doe');
  await user.type(screen.getByLabelText('Phone'), '0821234567');
  await user.type(screen.getByLabelText('Email'), 'jane@acme.example');

  await user.type(screen.getByLabelText('Street address'), '1 Main St');
  await user.type(screen.getByLabelText('Suburb'), 'CBD');
  await user.type(screen.getByLabelText('City'), 'Cape Town');
  await user.selectOptions(screen.getByLabelText('Province'), '1');
  await user.type(screen.getByLabelText('Postal code'), '8001');

  await user.selectOptions(screen.getByLabelText('Frequency'), '1');
  await user.type(screen.getByLabelText('Collection address'), '1 Main St, Cape Town');
  await user.click(screen.getByRole('button', { name: 'Fresh produce' }));
  await user.click(screen.getByRole('button', { name: 'Western Cape' }));
}

// These tests drive the full form with userEvent, which can exceed the default 5s under load (CI, full-suite runs).
describe('DonorForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  it('create mode: submits the filled form and redirects to the new donor detail page', async () => {
    // Realistic per-keystroke typing across every required field, plus this
    // is the first test in the file (pays jsdom/environment warm-up cost) —
    // routinely takes ~10-15s, well past the default 5000ms timeout.
    const user = userEvent.setup();
    mockApi();
    vi.mocked(api.post).mockResolvedValue({ data: { data: donorDetail({ id: 'new-donor-id' }) } });

    renderForm('create');
    await screen.findByLabelText('Company name');
    await fillRequiredFields(user);

    await user.click(screen.getByRole('button', { name: 'Create donor' }));

    await waitFor(() => expect(api.post).toHaveBeenCalledTimes(1));
    const [url, payload] = vi.mocked(api.post).mock.calls[0];
    expect(url).toBe('/api/v1/donors');
    expect(payload).toMatchObject({
      company: expect.objectContaining({ companyTypeId: 1, entityTypeId: 1, incomeTaxNumber: '9123456789' }),
      legalAddress: expect.objectContaining({ provinceId: 1 }),
      donations: expect.objectContaining({ frequencyId: 1, typeIds: [1], regionIds: [1] }),
    });

    expect(await screen.findByTestId('donor-detail')).toBeInTheDocument();
  }, 15000);

  it('blocks submission and shows an error when the income tax number starts with 4', async () => {
    const user = userEvent.setup();
    mockApi();

    renderForm('create');
    await screen.findByLabelText('Company name');
    await fillRequiredFields(user);

    await user.clear(screen.getByLabelText('Income tax number'));
    await user.type(screen.getByLabelText('Income tax number'), '4123456789');
    await user.click(screen.getByRole('button', { name: 'Create donor' }));

    expect(await screen.findByText('Income tax number cannot start with 4')).toBeInTheDocument();
    expect(api.post).not.toHaveBeenCalled();
  });

  it('edit mode: pre-populates from the existing donor and submits a PATCH on save', async () => {
    const user = userEvent.setup();
    const donor = donorDetail();
    mockApi({ donor });
    vi.mocked(api.patch).mockResolvedValue({ data: { data: donor } });

    renderForm('edit', donor.id);

    expect(await screen.findByDisplayValue('Acme Co')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Jane Doe')).toBeInTheDocument();
    expect(screen.getByDisplayValue('1 Main St')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(api.patch).toHaveBeenCalledTimes(1));
    expect(api.patch).toHaveBeenCalledWith(`/api/v1/donors/${donor.id}`, expect.objectContaining({
      company: expect.objectContaining({ companyName: 'Acme Co' }),
    }));
    expect(await screen.findByTestId('donor-detail')).toBeInTheDocument();
  });

  it('maps a 400 field-level error onto the matching input instead of a generic toast', async () => {
    const user = userEvent.setup();
    mockApi();
    vi.mocked(api.post).mockRejectedValue({
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          status: 400,
          code: 'VALIDATION_ERROR',
          message: 'One or more validation errors occurred.',
          errors: [{ field: 'Request.Company.CompanyRegistrationNumber', message: 'Invalid registration number.' }],
          traceId: 'trace-1',
        },
      },
    });

    renderForm('create');
    await screen.findByLabelText('Company name');
    await fillRequiredFields(user);

    await user.click(screen.getByRole('button', { name: 'Create donor' }));

    expect(await screen.findByText('Invalid registration number.')).toBeInTheDocument();
    expect(screen.getByText('Please fix the highlighted fields and try again.')).toBeInTheDocument();
  });
}, 20000);
