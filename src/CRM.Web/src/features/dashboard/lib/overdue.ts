import type { DonorFilters } from '@/features/donors/types';
import type { DashboardScopeFilters } from '../types';

/** yyyy-MM-dd for yesterday — the backend treats `followUpBefore` inclusively,
 * so this returns donors whose follow-up date is strictly before today. */
export function yesterdayYmd(): string {
  const d = new Date();
  d.setDate(d.getDate() - 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** Donor-list filters for "follow-up date is in the past", oldest-overdue first,
 * narrowed by the dashboard scope. Shared by OverdueFollowUpsWidget and the CSV export. */
export function overdueFollowUpFilters(scope: DashboardScopeFilters, pageSize: number): DonorFilters {
  return {
    ...scope,
    followUpBefore: yesterdayYmd(),
    sortBy: 'followUpDate',
    sortDir: 'asc',
    pageSize,
  };
}
