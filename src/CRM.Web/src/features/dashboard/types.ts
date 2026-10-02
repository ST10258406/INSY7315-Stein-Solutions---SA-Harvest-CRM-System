import type { DonorFilters } from '@/features/donors/types';

/**
 * Mirrors CRM.Application.Modules.Dashboard.Dtos.DashboardStatsDto — the
 * GET /api/v1/dashboard/stats response. `pendingApprovals` is forced to 0
 * server-side for any non-Admin role, so callers must still gate the card
 * with RoleGuard rather than trusting a non-zero value alone.
 */
export interface DashboardStatsDto {
  totalDonors: number;
  activeDonors: number;
  pendingApprovals: number;
  myOpenTasks: number;
  myOverdueFollowUps: number;
  donorsContactedThisMonth: number;
  donorsContactedLastMonth: number;
}

export type ManagerActivityPeriod = 'weekly' | 'monthly';

/** Mirrors CRM.Application.Modules.Dashboard.Dtos.ManagerActivityDto. */
export interface ManagerActivityDto {
  period: ManagerActivityPeriod;
  fromUtc: string;
  toUtc: string;
  items: { userId: string; name: string; donorsContacted: number }[];
}

/**
 * Donor-list filters the dashboard can narrow its donor-backed widgets by. A
 * subset of DonorFilters (GET /api/v1/donors) — the dashboard filters
 * client-side state only and adds no endpoint of its own.
 *
 * `scope` comes from the page-level Filters button and applies to every
 * donor-backed widget (Donor activity + Overdue follow-ups); `segment` comes
 * from the Donor activity section's own filter and narrows only its KPI cards.
 * The "Your work" stats and the Summary chart are server-aggregated per user /
 * per period, so neither is affected.
 */
export type DashboardScopeFilters = Pick<DonorFilters, 'relationshipManagerId' | 'regionCode'>;
export type DashboardSegmentFilters = Pick<DonorFilters, 'companyTypeId' | 'donationTypeId' | 'donationFrequencyId'>;
