import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { DonorKpiCards } from './DonorKpiCards';
import { ManagerActivityChart } from './ManagerActivityChart';
import { StatsCards } from './StatsCards';
import { OverdueFollowUpsWidget } from './OverdueFollowUpsWidget';
import { PendingApprovalsBanner } from './PendingApprovalsBanner';
import { DashboardScopeFilter, DonorActivityFilter } from './DashboardFilters';
import { DashboardExportMenu } from './DashboardExportMenu';
import type { DashboardScopeFilters, DashboardSegmentFilters, ManagerActivityPeriod } from './types';
import { paths } from '@/routes/paths';
import { buttonVariants } from '@/components/ui/button';

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  // Filter + period state lives here so the widgets, both filter popovers and the
  // export all read one source of truth (see DashboardScopeFilters for what applies where).
  const [scope, setScope] = useState<DashboardScopeFilters>({});
  const [segment, setSegment] = useState<DashboardSegmentFilters>({});
  const [period, setPeriod] = useState<ManagerActivityPeriod>('weekly');

  return (
    <main className="flex-1 min-w-0 overflow-y-auto p-[26px_30px_34px]">
      {/* Header Banner */}
      <div className="flex items-end gap-6 flex-wrap mb-6">
        <div>
          <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">
            Welcome back{user ? `, ` : ''}
            {user && <span className="text-[var(--ink)]">{user.firstName}</span>}!
          </h1>
          <p className="m-0 mt-1.5 text-sm font-medium text-[var(--muted-c)]">
            Track donor relationships, follow-ups, and approvals.
          </p>
        </div>

        <div className="ml-auto flex items-center gap-2.5">
          <DashboardScopeFilter value={scope} onChange={setScope} />

          <DashboardExportMenu scope={scope} segment={segment} period={period} />

          <Link
            to={paths.donorNew}
            className={buttonVariants({ variant: 'default', size: 'sm' })}
          >
            <Plus className="w-4 h-4 stroke-[2.2]" />
            <span>New Donor</span>
          </Link>
        </div>
      </div>

      {/* Admin-only, shown only when pending approvals exist (Issue #112) */}
      <PendingApprovalsBanner />

      {/* Top Stat Cards Section */}
      <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5 mb-5">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">
              Donor activity
            </h2>
            <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
              {Object.values({ ...scope, ...segment }).some((v) => v !== undefined)
                ? 'Live counts for donors matching the active filters.'
                : 'Live counts across every donor record in the system.'}
            </p>
          </div>
          <div className="flex gap-2">
            <DonorActivityFilter value={segment} onChange={setSegment} />
          </div>
        </div>

        <DonorKpiCards filters={{ ...scope, ...segment }} />
      </section>

      {/* Your Work Section (Issue #71): tasks, follow-ups, approvals, contacted-donor counts */}
      <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5 mb-5">
        <div className="mb-4">
          <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">
            Your work
          </h2>
          <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
            Tasks, follow-ups, and approvals that need your attention.
          </p>
        </div>

        <StatsCards />
      </section>

      {/* Main Grid: Summary Chart + Overdue Table */}
      <div className="grid grid-cols-1 lg:grid-cols-[minmax(360px,0.85fr)_1.3fr] gap-5 items-start">
        <ManagerActivityChart period={period} onPeriodChange={setPeriod} />

        {/* Right Section: Overdue Follow-Ups (Issue #111) */}
        <OverdueFollowUpsWidget filters={scope} />
      </div>
    </main>
  );
}
