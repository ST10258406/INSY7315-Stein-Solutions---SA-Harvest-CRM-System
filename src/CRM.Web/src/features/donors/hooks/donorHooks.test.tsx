import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { useDonors } from './useDonors';
import { useDonor } from './useDonor';
import { useCreateDonor } from './useCreateDonor';
import { useUpdateDonor } from './useUpdateDonor';
import { donorKeys } from './donorKeys';
import type { DonorDetailDto, DonorListItemDto, PaginatedResult } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}));

const donorListItem: DonorListItemDto = {
  id: 'donor-1',
  companyName: 'Acme Co',
  companyType: 'Retailer',
  status: 'Active',
  submissionSource: 'ManualCapture',
  relationshipManager: null,
  followUpDate: null,
  lastInteractionDate: null,
  lastInteractionType: null,
  operationalRegions: [],
  donationFrequency: null,
  donationTypes: [],
};

const donorDetail: DonorDetailDto = {
  id: 'donor-1',
  status: 'Active',
  submissionSource: 'ManualCapture',
  foodspaceCompanyId: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  company: {
    companyName: 'Acme Co',
    companyType: { id: 1, name: 'Retailer' },
    website: null,
    registeredCompanyName: 'Acme Co (Pty) Ltd',
    tradingName: null,
    entityType: { id: 1, name: 'Private Company' },
    companyRegistrationNumber: null,
    incomeTaxNumber: null,
  },
  primaryContact: { name: 'Jane Doe', jobTitle: null, phone: null, email: null },
  marketingContact: null,
  accountsContact: null,
  legalAddress: null,
  donations: {
    frequency: { id: 1, name: 'Weekly' },
    types: [],
    collectionAddress: null,
    operationsLogisticsDetails: null,
    operationalRegions: [],
  },
  compliance: { bbbeeStatus: null, documents: [] },
  crm: {
    relationshipManager: null,
    marketingConsent: false,
    marketingConsentDate: null,
    impactReportingPreferences: null,
    followUpDate: null,
    additionalInformation: null,
  },
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

describe('donor query hooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('useDonors → GETs /api/v1/donors with filters as params and returns the paginated result', async () => {
    const result: PaginatedResult<DonorListItemDto> = {
      data: [donorListItem],
      pagination: { page: 1, pageSize: 20, totalCount: 1, totalPages: 1 },
    };
    vi.mocked(api.get).mockResolvedValueOnce({ data: result });

    const { wrapper } = createWrapper();
    const { result: hookResult } = renderHook(() => useDonors({ search: 'Acme', page: 1 }), { wrapper });

    await waitFor(() => expect(hookResult.current.isSuccess).toBe(true));

    expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { search: 'Acme', page: 1 } });
    expect(hookResult.current.data).toEqual(result);
  });

  it('useDonor → GETs /api/v1/donors/{id} and unwraps the { data } envelope when enabled', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: { data: donorDetail } });

    const { wrapper } = createWrapper();
    const { result: hookResult } = renderHook(() => useDonor('donor-1'), { wrapper });

    await waitFor(() => expect(hookResult.current.isSuccess).toBe(true));

    expect(api.get).toHaveBeenCalledWith('/api/v1/donors/donor-1');
    expect(hookResult.current.data).toEqual(donorDetail);
  });

  it('useDonor → does not fire when id is undefined', () => {
    const { wrapper } = createWrapper();
    const { result: hookResult } = renderHook(() => useDonor(undefined), { wrapper });

    expect(hookResult.current.fetchStatus).toBe('idle');
    expect(api.get).not.toHaveBeenCalled();
  });

  it('useCreateDonor → POSTs to /api/v1/donors and invalidates the donors list on success', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: donorDetail } });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result: hookResult } = renderHook(() => useCreateDonor(), { wrapper });

    hookResult.current.mutate({
      company: {
        companyName: 'Acme Co',
        companyTypeId: 1,
        registeredCompanyName: 'Acme Co (Pty) Ltd',
        entityTypeId: 1,
      },
      primaryContact: { name: 'Jane Doe' },
      legalAddress: { streetAddress: '1 Main St', suburb: 'CBD', city: 'Cape Town', provinceId: 1, postalCode: '8001' },
      donations: { frequencyId: 1, typeIds: [1], collectionAddress: '1 Main St', regionIds: [1] },
    });

    await waitFor(() => expect(hookResult.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/donors', expect.any(Object));
    expect(hookResult.current.data).toEqual(donorDetail);
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.lists() });
  });

  it('useUpdateDonor → PATCHes /api/v1/donors/{id} and invalidates both the list and detail caches on success', async () => {
    vi.mocked(api.patch).mockResolvedValueOnce({ data: { data: donorDetail } });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result: hookResult } = renderHook(() => useUpdateDonor('donor-1'), { wrapper });

    hookResult.current.mutate({ company: { companyName: 'Acme Co Updated' } });

    await waitFor(() => expect(hookResult.current.isSuccess).toBe(true));

    expect(api.patch).toHaveBeenCalledWith('/api/v1/donors/donor-1', { company: { companyName: 'Acme Co Updated' } });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.lists() });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.detail('donor-1') });
  });

  it('useDonors → surfaces the standard API error envelope on failure', async () => {
    const apiError = {
      isAxiosError: true,
      response: {
        data: {
          status: 400,
          code: 'VALIDATION_ERROR',
          message: 'One or more validation errors occurred.',
          errors: [{ field: 'sortBy', message: 'sortBy must be one of: followUpDate, companyName, createdAt, lastInteractionDate' }],
          traceId: 'trace-1',
        },
      },
    };
    vi.mocked(api.get).mockRejectedValueOnce(apiError);

    const { wrapper } = createWrapper();
    const { result: hookResult } = renderHook(() => useDonors(), { wrapper });

    await waitFor(() => expect(hookResult.current.isError).toBe(true));

    expect(hookResult.current.error?.response?.data?.errors?.[0]).toEqual({
      field: 'sortBy',
      message: 'sortBy must be one of: followUpDate, companyName, createdAt, lastInteractionDate',
    });
  });
});
