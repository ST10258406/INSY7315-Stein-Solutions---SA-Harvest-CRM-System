import { renderHook, waitFor, cleanup } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { useDashboardStats } from './useDashboardStats';
import type { DashboardStatsDto } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
  },
}));

const STATS: DashboardStatsDto = {
  totalDonors: 42,
  activeDonors: 30,
  pendingApprovals: 3,
  myOpenTasks: 5,
  myOverdueFollowUps: 2,
  donorsContactedThisMonth: 12,
  donorsContactedLastMonth: 9,
};

function createWrapper() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
  };
}

describe('useDashboardStats', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('GETs /api/v1/dashboard/stats and returns the unwrapped data', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { data: STATS } });
    const { wrapper } = createWrapper();

    const { result } = renderHook(() => useDashboardStats(), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.get).toHaveBeenCalledWith('/api/v1/dashboard/stats');
    expect(result.current.data).toEqual(STATS);
  });

  it('does not refetch on a second mount within the staleTime window', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { data: STATS } });
    const { wrapper } = createWrapper();

    const first = renderHook(() => useDashboardStats(), { wrapper });
    await waitFor(() => expect(first.result.current.isSuccess).toBe(true));
    first.unmount();

    const second = renderHook(() => useDashboardStats(), { wrapper });
    expect(second.result.current.isSuccess).toBe(true);

    expect(api.get).toHaveBeenCalledTimes(1);
  });
});
