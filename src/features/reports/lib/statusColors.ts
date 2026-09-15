import type { DonorStatus } from '@/features/donors/types';

// Same palette as DonorStatusBadge (features/donors/components/DonorStatusBadge.tsx)
// so a status reads as the same color in the donor table and in this chart.
// Keep the two in sync if that badge's colors ever change.
export const STATUS_COLORS: Record<DonorStatus, string> = {
  Active: '#10b981', // emerald-500
  PendingReview: '#f59e0b', // amber-500
  Lapsed: '#f43f5e', // rose-500
  Rejected: 'var(--muted-c)', // theme-aware neutral, matches the badge's muted-foreground
};

export const STATUS_LABELS: Record<DonorStatus, string> = {
  Active: 'Active',
  PendingReview: 'Pending review',
  Lapsed: 'Lapsed',
  Rejected: 'Rejected',
};

/** Backend's own display order (ReportsRepository.StatusDisplayOrder) — kept here too
 *  as a defensive fallback in case the response order ever drifts from it. */
export const STATUS_ORDER: DonorStatus[] = ['Active', 'PendingReview', 'Lapsed', 'Rejected'];

const FALLBACK_COLOR = '#9ca3af'; // neutral grey for a status the app doesn't recognise yet

export function statusColor(status: string): string {
  return STATUS_COLORS[status as DonorStatus] ?? FALLBACK_COLOR;
}

export function statusLabel(status: string): string {
  return STATUS_LABELS[status as DonorStatus] ?? status;
}

export function sortByStatusOrder<T extends { status: string }>(rows: T[]): T[] {
  const rank = (s: string) => {
    const i = STATUS_ORDER.indexOf(s as DonorStatus);
    return i === -1 ? STATUS_ORDER.length : i;
  };
  return [...rows].sort((a, b) => rank(a.status) - rank(b.status));
}
