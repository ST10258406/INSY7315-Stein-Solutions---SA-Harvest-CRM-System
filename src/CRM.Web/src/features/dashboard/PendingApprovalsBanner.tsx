import { Link } from 'react-router-dom';
import { AlertTriangle, ArrowRight } from 'lucide-react';
import { paths } from '@/routes/paths';
import { RoleGuard } from '@/features/auth/components/RoleGuard';
import { useApprovals } from '@/features/approvals';

const ADMIN_ROLES = ['Admin', 'SuperAdmin'];

function PendingApprovalsBannerInner() {
  // pageSize 1 — we only need the count. Shares its cache key with nothing on
  // the approvals page (that one is paged), so it's one cheap standalone call.
  const { data } = useApprovals({ status: 'Pending', page: 1, pageSize: 1 });
  const pendingCount = data?.pagination.totalCount ?? 0;

  if (pendingCount === 0) return null;

  return (
    <Link
      to={paths.approvals}
      className="mb-5 flex items-center gap-3.5 rounded-2xl border border-[#EFD9A0] bg-[#FCF6D8] p-[14px_18px] transition-colors hover:border-[#E0C46A] dark:border-[#4A400C] dark:bg-[#241F05]"
    >
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#F7E9A8] text-[#8A6100] dark:bg-[#3A320A] dark:text-[#EBD98C]">
        <AlertTriangle className="h-4.5 w-4.5" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="m-0 text-[13.5px] font-extrabold tracking-tight text-[var(--ink)]">
          {pendingCount} donor {pendingCount === 1 ? 'submission is' : 'submissions are'} waiting for review
        </p>
        <p className="m-0 mt-0.5 text-[12px] font-medium text-[var(--muted-c)]">
          New donors stay in Pending Review until an Admin approves them.
        </p>
      </div>
      <span className="flex shrink-0 items-center gap-1.5 text-[12.5px] font-bold text-[var(--ink)]">
        <span>Review now</span>
        <ArrowRight className="h-3.75 w-3.75" />
      </span>
    </Link>
  );
}

/**
 * Dashboard banner (Issue #112, deferred from Sprint 3). Admin-only and only
 * rendered when the pending approval count is greater than zero.
 */
export function PendingApprovalsBanner() {
  return (
    <RoleGuard allowedRoles={ADMIN_ROLES}>
      <PendingApprovalsBannerInner />
    </RoleGuard>
  );
}
