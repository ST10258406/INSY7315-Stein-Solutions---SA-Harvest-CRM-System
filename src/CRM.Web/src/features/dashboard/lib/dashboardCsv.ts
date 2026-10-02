import { csvJoin, csvRow as row } from '@/lib/csv';
import type { DonorListItemDto } from '@/features/donors/types';
import type { DashboardStatsDto, ManagerActivityDto } from '../types';

export interface DashboardSnapshot {
  generatedAt: Date;
  /** Human-readable [filter, value] pairs for every active dashboard filter. */
  filters: [string, string][];
  donorCounts: { total: number; active: number; pendingReview: number; lapsed: number };
  stats: DashboardStatsDto;
  /** Pending approvals is forced to 0 server-side for non-Admins — only include it for Admins. */
  includePendingApprovals: boolean;
  periodLabel: string;
  activity: ManagerActivityDto;
  overdue: DonorListItemDto[];
  overdueTotal: number;
}

/** The dashboard as on screen, flattened into one sectioned CSV. */
export function buildDashboardCsv(s: DashboardSnapshot): string {
  const lines: string[] = [
    row('SA Harvest CRM — Dashboard export'),
    row('Generated', s.generatedAt.toLocaleString('en-ZA')),
    row('Filters', s.filters.length ? s.filters.map(([k, v]) => `${k}: ${v}`).join('; ') : 'None'),
    '',
    row('Donor activity'),
    row('Metric', 'Count'),
    row('Total donors', s.donorCounts.total),
    row('Active donors', s.donorCounts.active),
    row('Pending review', s.donorCounts.pendingReview),
    row('Lapsed donors', s.donorCounts.lapsed),
    '',
    row('Your work'),
    row('Metric', 'Count'),
  ];

  if (s.includePendingApprovals) lines.push(row('Pending approvals', s.stats.pendingApprovals));
  lines.push(
    row('My open tasks', s.stats.myOpenTasks),
    row('My overdue follow-ups', s.stats.myOverdueFollowUps),
    row('Donors contacted this month', s.stats.donorsContactedThisMonth),
    row('Donors contacted last month', s.stats.donorsContactedLastMonth),
    '',
    row(`Donors contacted per team member (${s.periodLabel})`),
    row('Team member', 'Donors contacted'),
    ...s.activity.items.map((item) => row(item.name, item.donorsContacted)),
    '',
    row(
      s.overdueTotal > s.overdue.length
        ? `Overdue follow-ups (first ${s.overdue.length} of ${s.overdueTotal})`
        : 'Overdue follow-ups',
    ),
    row('Donor', 'Status', 'Relationship manager', 'Follow-up date'),
    ...s.overdue.map((d) =>
      row(d.companyName, d.status, d.relationshipManager?.fullName ?? '', d.followUpDate?.slice(0, 10) ?? ''),
    ),
  );

  return csvJoin(lines);
}
