import type { ApprovalFilters } from '../types';

/**
 * Query-key factory for the approval queue. `approvalKeys.all` invalidates every
 * status/page variation at once — used after an approve/reject so all three tabs
 * refetch.
 */
export const approvalKeys = {
  all: ['approvals'] as const,
  list: (filters: ApprovalFilters = {}) => [...approvalKeys.all, 'list', filters] as const,
};
