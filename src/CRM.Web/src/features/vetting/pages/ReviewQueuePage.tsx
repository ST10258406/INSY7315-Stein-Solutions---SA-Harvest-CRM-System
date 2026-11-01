import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ListPagination } from '@/components/common/ListPagination';
import { paths } from '@/routes/paths';
import { useVettingSubmissions } from '../hooks';
import { TD, TH, VettingTableStates } from '../components/VettingTableStates';

const DATE = new Intl.DateTimeFormat('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });

/** /vetting/review — Admin queue of Ready submissions, longest-waiting first. */
export default function ReviewQueuePage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const { data, isPending, isError, error, refetch } = useVettingSubmissions({
    approvalState: 'Ready',
    sortBy: 'submittedAt',
    sortDir: 'asc',
    page,
    pageSize,
  });
  const rows = data?.data ?? [];

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_40px]">
      <div className="mb-5">
        <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Vetting reviews</h1>
        <p className="m-0 text-sm font-medium text-[var(--muted-c)]">Submitted vettings waiting for a decision, longest-waiting first.</p>
      </div>

      <div className="overflow-x-auto rounded-xl bg-[var(--card)] shadow-[0_1px_3px_var(--shadow)]">
        <table className="w-full min-w-[860px] border-collapse">
          <thead>
            <tr className="bg-[var(--hair)]">
              <th className={TH}>Form reference</th>
              <th className={TH}>Donor</th>
              <th className={TH}>Province</th>
              <th className={TH}>Officer</th>
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
              isEmpty={rows.length === 0}
              emptyText="No vettings waiting for review."
            />
            {rows.map((s) => (
              <tr key={s.id} onClick={() => navigate(paths.vettingDetail(s.id))} className="cursor-pointer hover:bg-hover">
                <td className={TD}>
                  {/* The link keeps the row reachable by keyboard; the row click is a mouse shortcut. */}
                  <Link to={paths.vettingDetail(s.id)} className="font-bold hover:underline" onClick={(e) => e.stopPropagation()}>
                    {s.formReference}
                  </Link>
                </td>
                <td className={TD}>{s.donorCompanyName}</td>
                <td className={TD}>{s.province}</td>
                <td className={TD}>{s.officerFullName}</td>
                <td className={TD}>{s.submittedAt ? DATE.format(new Date(s.submittedAt)) : '—'}</td>
                <td className={TD}>
                  <span
                    className={`rounded-full px-2.5 py-0.5 text-xs font-bold ${
                      s.activeFlagCount > 0 ? 'bg-destructive/10 text-destructive' : 'text-muted-foreground'
                    }`}
                  >
                    {s.activeFlagCount}
                  </span>
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
            itemLabel="vettings"
          />
        </div>
      )}
    </main>
  );
}
