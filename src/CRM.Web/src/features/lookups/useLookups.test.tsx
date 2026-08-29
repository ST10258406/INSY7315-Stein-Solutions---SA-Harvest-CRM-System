import { renderHook, waitFor, cleanup } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import {
  useCompanyTypes,
  useEntityTypes,
  useOperationalRegions,
  useDonationTypes,
  useDonationFrequencies,
  useProvinces,
  useBbbeeStatuses,
  useDonorLookups,
} from './useLookups';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
  },
}));

const ENDPOINTS: Record<string, unknown[]> = {
  '/api/v1/lookups/company-types': [{ id: 1, name: 'Retailer' }],
  '/api/v1/lookups/entity-types': [{ id: 1, name: 'Private Company' }],
  '/api/v1/lookups/operational-regions': [{ id: 1, code: 'WC', name: 'Western Cape' }],
  '/api/v1/lookups/donation-types': [{ id: 1, name: 'Fresh produce' }],
  '/api/v1/lookups/donation-frequencies': [{ id: 1, name: 'Weekly' }],
  '/api/v1/lookups/provinces': [{ id: 1, code: 'WC', name: 'Western Cape' }],
  '/api/v1/lookups/bbbee-statuses': [{ id: 1, name: 'Level 2' }],
};

function mockApi() {
  vi.mocked(api.get).mockImplementation((url: string) =>
    Promise.resolve({ data: { data: ENDPOINTS[url] ?? [] } })
  );
}

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
  };
}

describe('donor lookup hooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  // Loosely typed on purpose: the 7 hooks return different list element
  // types (LookupDto[] vs RegionDto[] vs ProvinceDto[]) but every case here
  // only touches the shape common to all of them.
  type LookupQueryResult = { isSuccess: boolean; data: unknown };
  const cases: Array<[string, () => LookupQueryResult, string]> = [
    ['useCompanyTypes', useCompanyTypes, '/api/v1/lookups/company-types'],
    ['useEntityTypes', useEntityTypes, '/api/v1/lookups/entity-types'],
    ['useOperationalRegions', useOperationalRegions, '/api/v1/lookups/operational-regions'],
    ['useDonationTypes', useDonationTypes, '/api/v1/lookups/donation-types'],
    ['useDonationFrequencies', useDonationFrequencies, '/api/v1/lookups/donation-frequencies'],
    ['useProvinces', useProvinces, '/api/v1/lookups/provinces'],
    ['useBbbeeStatuses', useBbbeeStatuses, '/api/v1/lookups/bbbee-statuses'],
  ];

  it.each(cases)('%s → GETs %s through lib/axios and returns the unwrapped list', async (_name, useHook, url) => {
    mockApi();
    const { wrapper } = createWrapper();

    const { result } = renderHook(() => useHook(), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.get).toHaveBeenCalledWith(url);
    expect(result.current.data).toEqual(ENDPOINTS[url]);
  });

  it.each(cases)('%s → caches with a 24h staleTime/gcTime, so a second mount does not refetch', async (_name, useHook) => {
    mockApi();
    const { wrapper } = createWrapper();

    const first = renderHook(() => useHook(), { wrapper });
    await waitFor(() => expect(first.result.current.isSuccess).toBe(true));
    first.unmount();

    // Same QueryClient (same cache), fresh component mount — this is the
    // automated equivalent of "verify via devtools that a second mount
    // doesn't trigger a refetch" from the acceptance criteria, but one that
    // actually runs in CI.
    const second = renderHook(() => useHook(), { wrapper });
    expect(second.result.current.isSuccess).toBe(true);
    expect(second.result.current.data).toEqual(first.result.current.data);

    expect(api.get).toHaveBeenCalledTimes(1);
  });

  it('useDonorLookups → fetches all 7 lookups together, one request per endpoint', async () => {
    mockApi();
    const { wrapper } = createWrapper();

    const { result } = renderHook(() => useDonorLookups(), { wrapper });

    await waitFor(() => {
      expect(result.current.companyTypes.isSuccess).toBe(true);
      expect(result.current.entityTypes.isSuccess).toBe(true);
      expect(result.current.operationalRegions.isSuccess).toBe(true);
      expect(result.current.donationTypes.isSuccess).toBe(true);
      expect(result.current.donationFrequencies.isSuccess).toBe(true);
      expect(result.current.provinces.isSuccess).toBe(true);
      expect(result.current.bbbeeStatuses.isSuccess).toBe(true);
    });

    expect(result.current.companyTypes.data).toEqual(ENDPOINTS['/api/v1/lookups/company-types']);
    expect(result.current.bbbeeStatuses.data).toEqual(ENDPOINTS['/api/v1/lookups/bbbee-statuses']);
    expect(api.get).toHaveBeenCalledTimes(Object.keys(ENDPOINTS).length);
  });

  it('useDonorLookups → warms the cache so a lookup used individually afterwards does not refetch', async () => {
    mockApi();
    const { wrapper } = createWrapper();

    const combined = renderHook(() => useDonorLookups(), { wrapper });
    await waitFor(() => expect(combined.result.current.companyTypes.isSuccess).toBe(true));
    const callsAfterWarm = vi.mocked(api.get).mock.calls.length;

    const individual = renderHook(() => useCompanyTypes(), { wrapper });
    expect(individual.result.current.isSuccess).toBe(true);
    expect(individual.result.current.data).toEqual(ENDPOINTS['/api/v1/lookups/company-types']);

    expect(api.get).toHaveBeenCalledTimes(callsAfterWarm);
  });
});
