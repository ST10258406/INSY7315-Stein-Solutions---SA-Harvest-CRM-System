import type { ApiError, PaginatedResult } from '@/features/donors/types';

export type { ApiError, PaginatedResult };

// Mirrors CRM.Domain.Enums.ApprovalStatus.
export const APPROVAL_STATUSES = ['Pending', 'Approved', 'Rejected'] as const;
export type ApprovalStatus = (typeof APPROVAL_STATUSES)[number];

export interface ApprovalUserDto {
  id: string;
  fullName: string;
}

export interface ApprovalDonorDto {
  id: string;
  companyName: string;
  status: string;
}

/** One row of the donor approval queue. Mirrors ApprovalDto. */
export interface ApprovalDto {
  id: string;
  status: string;
  rejectionReason: string | null;
  reviewedAt: string | null;
  createdAt: string;
  donor: ApprovalDonorDto;
  requestedBy: ApprovalUserDto | null;
  reviewedBy: ApprovalUserDto | null;
}

/** Mirrors GetApprovalsQuery. */
export interface ApprovalFilters {
  status?: ApprovalStatus;
  page?: number;
  pageSize?: number;
}

/** Input to useApprovalAction — one hook for both decisions. */
export type ApprovalActionInput =
  | { approvalId: string; action: 'approve' }
  | { approvalId: string; action: 'reject'; rejectionReason: string };
