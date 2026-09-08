import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { useApprovals } from './useApprovals';
import { useApprovalAction } from './useApprovalAction';
import { approvalKeys } from './approvalKeys';
import type { ApprovalDto, PaginatedResult } from '../types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), post: vi.fn() } }));

const approval: ApprovalDto = {
  id: 'appr-1',
  status: 'Pending',
  rejectionReason: null,
  reviewedAt: null,
  createdAt: '2026-09-01T09:00:00Z',
  donor: { id: 'donor-1', companyName: 'FoodCorp SA', status: 'PendingReview' },
  requestedBy: { id: 'user-1', fullName: 'Riaan Botha' },
  reviewedBy: null,
};

const page: PaginatedResult<ApprovalDto> = {
  data: [approval],
  pagination: { page: 1, pageSize: 20, totalCount: 1, totalPages: 1 },
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

describe('approval hooks', () => {
  beforeEach(() => vi.clearAllMocks());

  it('useApprovals → GETs /api/v1/approvals with the status filter', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: page });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useApprovals({ status: 'Pending', page: 1, pageSize: 20 }), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.get).toHaveBeenCalledWith('/api/v1/approvals', {
      params: { status: 'Pending', page: 1, pageSize: 20 },
    });
    expect(result.current.data?.data[0].donor.companyName).toBe('FoodCorp SA');
  });

  it('useApprovalAction → approve POSTs /approve and invalidates approvals + donors', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ status: 204 });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useApprovalAction(), { wrapper });

    result.current.mutate({ approvalId: 'appr-1', action: 'approve' });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/approvals/appr-1/approve');
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: approvalKeys.all });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.all });
  });

  it('useApprovalAction → reject POSTs /reject with the reason in the body', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ status: 204 });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useApprovalAction(), { wrapper });

    result.current.mutate({ approvalId: 'appr-1', action: 'reject', rejectionReason: 'Registration number mismatch.' });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/approvals/appr-1/reject', {
      rejectionReason: 'Registration number mismatch.',
    });
  });
});
