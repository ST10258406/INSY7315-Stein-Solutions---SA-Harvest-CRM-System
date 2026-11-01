import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDown, Plus } from 'lucide-react';
import { ListPagination } from '@/components/common/ListPagination';
import { Button } from '@/components/ui/button';
import { paths } from '@/routes/paths';
import { useVettingSubmissions } from '../hooks';
import { VettingStatusBadge } from '../components/VettingStatusBadge';
import { TD, TH, VettingTableStates } from '../components/VettingTableStates';
import { APPROVAL_STATE_LABELS } from '../lib/vettingLabels';
import type { VettingApprovalState } from '../types';

const DATE = new Intl.DateTimeFormat('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });

/** /vetting — every submission, newest first, filterable by approval state. */
export default function VettingListPage() {
  const [approvalState, setApprovalState] = useState<VettingApprovalState | ''>('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const { data, isPending, isError, error, refetch } = useVettingSubmissions({
    page,
    pageSize,
    ...(approvalState ? { approvalState } : {}),
  });
  const submissions = data?.data ?? [];

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_40px]">
      <div className="mb-5 flex flex-wrap items-end gap-6">
        <div>
          <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Vetting</h1>
          <p className="m-0 text-sm font-medium text-[var(--muted-c)]">Site audits of donors, from first save through admin review.</p>
        </div>
        <div className="ml-auto">
          <Button size="sm" render={<Link to={paths.vettingNew} />}>
            <Plus className="h-4 w-4 stroke-[2.2]" />
            <span>New vetting</span>
          </Button>
        </div>
      </div>

      <section className="mb-4.5 flex flex-wrap items-center gap-3 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4">
        <div className="relative">
          <select
            aria-label="Approval state"
            value={approvalState}
            onChange={(e) => {
              setApprovalState(e.target.value as VettingApprovalState | '');
              setPage(1);
            }}
            className="h-10 appearance-none rounded-full border border-border bg-field pl-3.75 pr-9 text-[13px] font-semibold text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60"
          >
            <option value="">All states</option>
            {Object.entries(APPROVAL_STATE_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
          <ChevronDown className="pointer-events-none absolute top-3.25 right-3.5 h-3.5 w-3.5 text-muted-foreground" />
        </div>
      </section>

      <div className="overflow-x-auto rounded-xl bg-[var(--card)] shadow-[0_1px_3px_var(--shadow)]">
        <table className="w-full min-w-[860px] border-collapse">
          <thead>
            <tr className="bg-[var(--hair)]">
              <th className={TH}>Form reference</th>
              <th className={TH}>Donor</th>
              <th className={TH}>Officer</th>
              <th className={TH}>State</th>
              <th className={TH}>Submitted</th>
              <th className={TH}>Flags</th>
            </tr>
          </thead>
          <tbody>
            <VettingTableStates
              colSpan={6}
              isPending={isPending}
              isError={isError}
              error={error}
              onRetry={refetch}
              isEmpty={submissions.length === 0}
              emptyText="No vetting submissions yet."
            />
            {submissions.map((s) => (
              <tr key={s.id}>
                <td className={TD}>
                  <Link to={paths.vettingDetail(s.id)} className="font-bold hover:underline">
                    {s.formReference}
                  </Link>
                </td>
                <td className={TD}>{s.donorCompanyName}</td>
                <td className={TD}>{s.officerFullName}</td>
                <td className={TD}>
                  <VettingStatusBadge kind="approval" status={s.approvalState} />
                </td>
                <td className={TD}>{s.submittedAt ? DATE.format(new Date(s.submittedAt)) : '—'}</td>
                <td className={`${TD} ${s.activeFlagCount > 0 ? 'font-bold text-amber-700 dark:text-amber-400' : 'text-muted-foreground'}`}>
                  {s.activeFlagCount}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {data && data.pagination.totalCount > 0 && (
        <div className="mt-4">
          <ListPagination
            pagination={data.pagination}
            onPageChange={setPage}
            onPageSizeChange={(next) => {
              setPageSize(next);
              setPage(1);
            }}
            itemLabel="submissions"
          />
        </div>
      )}
    </main>
  );
}
