import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { approvalKeys } from './approvalKeys';
import type { ApiError, ApprovalDto, ApprovalFilters, PaginatedResult } from '../types';

/**
 * GET /api/v1/approvals — the donor approval queue (Admin only). Defaults to
 * Pending, newest first. Returns the `PaginatedResult` envelope directly.
 */
export function useApprovals(filters: ApprovalFilters = {}) {
  return useQuery<PaginatedResult<ApprovalDto>, ApiError>({
    queryKey: approvalKeys.list(filters),
    queryFn: async () => {
      const { data } = await api.get<PaginatedResult<ApprovalDto>>('/api/v1/approvals', { params: filters });
      return data;
    },
    placeholderData: keepPreviousData,
  });
}
