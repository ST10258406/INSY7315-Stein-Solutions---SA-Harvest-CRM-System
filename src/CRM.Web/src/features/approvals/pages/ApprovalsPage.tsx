import { useState } from 'react';
import { Lock } from 'lucide-react';
import { ListPagination } from '@/components/common/ListPagination';
import { useApprovals } from '../hooks';
import { ApprovalQueue } from '../components/ApprovalQueue';
import { RejectDonorDialog } from '../components/RejectDonorDialog';
import { APPROVAL_STATUSES, type ApprovalDto, type ApprovalStatus } from '../types';

const DEFAULT_PAGE_SIZE = 20;

export default function ApprovalsPage() {
  const [status, setStatus] = useState<ApprovalStatus>('Pending');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [rejecting, setRejecting] = useState<ApprovalDto | null>(null);

  const { data, isPending, isError, error, refetch } = useApprovals({ status, page, pageSize });
  const approvals = data?.data ?? [];
  const pagination = data?.pagination;

  const changePageSize = (next: number) => {
    setPageSize(next);
    setPage(1);
  };

  const changeStatus = (next: ApprovalStatus) => {
    setStatus(next);
    setPage(1);
  };

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_40px]">
      <div className="mb-4.5 flex flex-wrap items-end gap-6">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Approvals</h1>
            {status === 'Pending' && pagination && (
              <span
                className={`inline-flex h-8 min-w-[32px] items-center justify-center rounded-2xl px-2.75 text-sm font-extrabold ${
                  pagination.totalCount ? 'bg-[var(--ink)] text-[var(--brand-yellow)]' : 'bg-[var(--chip)] text-[var(--muted-c)]'
                }`}
              >
                {pagination.totalCount}
              </span>
            )}
          </div>
          <p className="m-0 mt-1.75 text-sm font-medium text-[var(--muted-c)]">
            Review and approve new donor submissions before they go live.
          </p>
        </div>

        <div className="ml-auto flex h-9 items-center gap-2.25 rounded-2xl border border-[var(--border)] bg-[var(--card)] px-3.5 shadow-[0_1px_2px_var(--shadow)]">
          <Lock className="h-3.75 w-3.75 text-[var(--icon)]" />
          <span className="text-[12px] font-bold tracking-wide text-[var(--muted-c)]">ADMIN ONLY</span>
        </div>
      </div>

      <div className="mb-5 flex items-center gap-1 overflow-x-auto border-b border-[var(--border)]">
        {APPROVAL_STATUSES.map((value) => {
          const active = status === value;
          return (
            <button
              key={value}
              type="button"
              onClick={() => changeStatus(value)}
              className={`-mb-px flex h-11 items-center gap-2 whitespace-nowrap border-b-[3px] px-4.5 text-[13.5px] transition-colors ${
                active
                  ? 'border-[var(--ink)] font-extrabold text-[var(--ink)]'
                  : 'border-transparent font-semibold text-[var(--muted-c)] hover:text-[var(--ink)]'
              }`}
            >
              <span>{value}</span>
              {active && pagination && (
                <span className="inline-flex h-5 min-w-[20px] items-center justify-center rounded-[10px] bg-[var(--ink)]/10 px-1.5 text-[10.5px] font-bold text-[var(--ink)]">
                  {pagination.totalCount}
                </span>
              )}
            </button>
          );
        })}
      </div>

      <ApprovalQueue
        approvals={approvals}
        isPending={isPending}
        isError={isError}
        error={error}
        onRetry={refetch}
        onReject={setRejecting}
        status={status}
      />

      {pagination && pagination.totalCount > 0 && (
        <ListPagination
          pagination={pagination}
          onPageChange={setPage}
          onPageSizeChange={changePageSize}
          itemLabel="approvals"
        />
      )}

      <RejectDonorDialog approval={rejecting} open={!!rejecting} onOpenChange={(next) => !next && setRejecting(null)} />
    </main>
  );
}
