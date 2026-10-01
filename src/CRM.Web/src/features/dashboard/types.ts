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
