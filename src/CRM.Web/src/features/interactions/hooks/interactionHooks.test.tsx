import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { useInteractions } from './useInteractions';
import { useLogInteraction } from './useLogInteraction';
import { interactionKeys } from './interactionKeys';
import type { InteractionLogDto, PaginatedResult } from '../types';

vi.mock('@/lib/axios', () => ({
  api: { get: vi.fn(), post: vi.fn() },
}));

const row: InteractionLogDto = {
  id: 'int-1',
  donorId: 'donor-1',
  interactionType: 'Call',
  subject: 'Surplus forecast',
  body: 'Confirmed weekly volume.',
  emailAttachmentUrl: null,
  createdAt: '2026-08-01T09:00:00Z',
  createdBy: { id: 'user-1', fullName: 'Nomsa Khumalo' },
};

const page = (pageNo: number, totalPages: number): PaginatedResult<InteractionLogDto> => ({
  data: [{ ...row, id: `int-${pageNo}` }],
  pagination: { page: pageNo, pageSize: 20, totalCount: totalPages * 20, totalPages },
});

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

describe('interaction hooks', () => {
  beforeEach(() => vi.clearAllMocks());

  it('useInteractions → GETs the donor-scoped endpoint with page/pageSize params', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: page(1, 1) });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useInteractions('donor-1', { interactionType: 'Call' }), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.get).toHaveBeenCalledWith('/api/v1/donors/donor-1/interactions', {
      params: { page: 1, pageSize: 20, interactionType: 'Call' },
    });
    expect(result.current.data?.pages[0].data[0].subject).toBe('Surplus forecast');
  });

  it('useInteractions → does not fire without a donor id', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useInteractions(undefined), { wrapper });
    expect(result.current.fetchStatus).toBe('idle');
    expect(api.get).not.toHaveBeenCalled();
  });

  it('useInteractions → exposes a next page only while page < totalPages', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: page(1, 2) });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useInteractions('donor-1'), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.hasNextPage).toBe(true);

    vi.mocked(api.get).mockResolvedValueOnce({ data: page(2, 2) });
    await result.current.fetchNextPage();
    await waitFor(() => expect(result.current.hasNextPage).toBe(false));
  });

  it('useLogInteraction → POSTs the request, unwraps { data }, invalidates timeline + donor detail', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: row } });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useLogInteraction('donor-1'), { wrapper });

    result.current.mutate({ interactionType: 'Call', body: 'Confirmed weekly volume.' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/donors/donor-1/interactions', {
      interactionType: 'Call',
      body: 'Confirmed weekly volume.',
    });
    expect(result.current.data).toEqual(row);
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: interactionKeys.donor('donor-1') });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.detail('donor-1') });
  });
});
