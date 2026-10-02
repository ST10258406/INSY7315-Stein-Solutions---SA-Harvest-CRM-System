import { renderHook, waitFor, cleanup } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { downloadCsv } from '@/lib/csv';
import { useExportDonors } from './useExportDonors';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));
vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));
vi.mock('@/lib/csv', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/csv')>()),
  downloadCsv: vi.fn(),
}));

function page(n: number, totalPages: number) {
  return {
    data: {
      data: [{ id: `d${n}`, companyName: `Donor ${n}`, companyType: 'Retailer', status: 'Active', submissionSource: 'ManualCapture', relationshipManager: null, followUpDate: null, lastInteractionDate: null, lastInteractionType: null, operationalRegions: [], donationFrequency: null, donationTypes: [] }],
      pagination: { page: n, pageSize: 100, totalCount: totalPages, totalPages },
    },
  };
}

function wrapper({ children }: { children: ReactNode }) {
  return <QueryClientProvider client={new QueryClient({ defaultOptions: { mutations: { retry: false } } })}>{children}</QueryClientProvider>;
}

describe('useExportDonors', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('pages through every matching donor with the current filters and downloads one CSV', async () => {
    vi.mocked(api.get).mockImplementation((_url, config) =>
      Promise.resolve(page((config?.params as { page: number }).page, 3)),
    );

    const { result } = renderHook(() => useExportDonors(), { wrapper });
    result.current.mutate({ status: 'Active', page: 4, pageSize: 20 });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.get).toHaveBeenCalledTimes(3);
    for (const n of [1, 2, 3]) {
      expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { status: 'Active', page: n, pageSize: 100 } });
    }
    const [csv, fileName] = vi.mocked(downloadCsv).mock.calls[0];
    expect(csv.split('\r\n')).toHaveLength(4); // header + 3 donors
    expect(fileName).toMatch(/^sa-harvest-donors-\d{4}-\d{2}-\d{2}\.csv$/);
    expect(result.current.data).toBe(3);
  });
});
