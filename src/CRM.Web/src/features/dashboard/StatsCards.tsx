import { ClipboardCheck, ListTodo, CalendarClock, PhoneCall } from 'lucide-react';
import { RoleGuard } from '@/features/auth/components/RoleGuard';
import { useDashboardStats } from './hooks';
import { StatsCard } from './StatsCard';

const ADMIN_ROLES = ['Admin', 'SuperAdmin'];

/**
 * Dashboard Phase 2 (Issue #71): the KPI cards left as placeholders in Phase 1
 * because their data depended on Sprint 4/5 APIs. All four share the one
 * GET /api/v1/dashboard/stats call via useDashboardStats instead of each
 * firing its own request.
 */
export function StatsCards() {
  const { data, isPending, isError } = useDashboardStats();

  return (
    <div className="flex flex-wrap gap-4">
      {/* The backend forces pendingApprovals to 0 for non-Admins, so hide the
          card entirely for them instead of showing a permanently-zero tile. */}
      <RoleGuard allowedRoles={ADMIN_ROLES}>
        <StatsCard
          icon={ClipboardCheck}
          label="Pending Approvals"
          value={data?.pendingApprovals}
          isLoading={isPending}
          isError={isError}
        />
      </RoleGuard>

      <StatsCard
        icon={ListTodo}
        label="My Open Tasks"
        value={data?.myOpenTasks}
        isLoading={isPending}
        isError={isError}
      />

      <StatsCard
        icon={CalendarClock}
        label="My Overdue Follow-Ups"
        value={data?.myOverdueFollowUps}
        isLoading={isPending}
        isError={isError}
      />

      <StatsCard
        icon={PhoneCall}
        label="Donors Contacted This Month"
        value={data?.donorsContactedThisMonth}
        isLoading={isPending}
        isError={isError}
      />
    </div>
  );
}
