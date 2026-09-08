import { Link } from 'react-router-dom';
import { CalendarCheck, TriangleAlert } from 'lucide-react';
import { paths } from '@/routes/paths';
import { useDonors } from '@/features/donors/hooks';
import { getInitials, getAvatarColor } from '@/features/donors/lib/avatar';
import { formatDate } from '@/features/donors/lib/donorFormatters';

const ROW_LIMIT = 6;

/** yyyy-MM-dd for yesterday — the backend treats `followUpBefore` inclusively,
 * so this returns donors whose follow-up date is strictly before today. */
function yesterdayYmd(): string {
  const d = new Date();
  d.setDate(d.getDate() - 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function daysOverdue(value: string | null): number {
  if (!value) return 0;
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return 0;
  // Compare calendar days only — the wire value may be date-only or a full
  // timestamp, and either way "overdue by N days" should ignore time-of-day.
  const due = new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  return Math.max(0, Math.round((today.getTime() - due.getTime()) / 86_400_000));
}

/**
 * Deferred from Sprint 3 (Issue #111). Donors whose follow-up date is in the
 * past, newest-overdue first, straight off the donor list endpoint's
 * `followUpBefore` filter — no dedicated endpoint.
 */
export function OverdueFollowUpsWidget() {
  const { data, isPending, isError, refetch } = useDonors({
    followUpBefore: yesterdayYmd(),
    sortBy: 'followUpDate',
    sortDir: 'asc',
    pageSize: ROW_LIMIT,
  });

  const rows = data?.data ?? [];
  const totalCount = data?.pagination.totalCount ?? 0;

  return (
    <section className="min-w-0 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-5">
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">Overdue Follow-Ups</h2>
          <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
            {isPending
              ? 'Checking follow-up commitments…'
              : totalCount > 0
                ? `${totalCount} ${totalCount === 1 ? 'relationship is' : 'relationships are'} past the contact commitment.`
                : 'Every follow-up is on track.'}
          </p>
        </div>
      </div>

      <div className="rounded-xl bg-[var(--card)] p-[6px_18px_10px] shadow-[0_1px_3px_var(--shadow)]">
        {isPending ? (
          <div className="flex flex-col gap-2 py-4" aria-hidden>
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-11 animate-pulse rounded-lg bg-[var(--skel)]" />
            ))}
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 py-10 text-center">
            <TriangleAlert className="h-5 w-5 text-muted-foreground" />
            <p className="m-0 text-[13px] font-semibold text-foreground">Couldn't load overdue follow-ups</p>
            <button
              type="button"
              onClick={() => refetch()}
              className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
            >
              Retry
            </button>
          </div>
        ) : rows.length === 0 ? (
          <div className="flex flex-col items-center gap-2.5 py-10 text-center">
            <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-[#E5F4E9] text-[#1E6E3C]">
              <CalendarCheck className="h-5 w-5" />
            </span>
            <p className="m-0 text-[13px] font-bold tracking-tight text-foreground">Nothing overdue</p>
            <p className="m-0 text-[11.5px] font-medium text-muted-foreground">
              Every donor with a follow-up date is still within its window.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[520px] border-collapse">
              <thead>
                <tr>
                  <th className="py-3.5 pr-2 pl-0 text-left text-[11.5px] font-semibold text-[var(--muted2)]">Donor</th>
                  <th className="py-3.5 pr-2 pl-0 text-left text-[11.5px] font-semibold text-[var(--muted2)]">
                    Follow-up date
                  </th>
                  <th className="py-3.5 pr-2 pl-0 text-left text-[11.5px] font-semibold text-[var(--muted2)]">Overdue</th>
                  <th className="py-3.5 pr-0 pl-2 text-right text-[11.5px] font-semibold text-[var(--muted2)]" />
                </tr>
              </thead>
              <tbody>
                {rows.map((donor) => {
                  const days = daysOverdue(donor.followUpDate);
                  return (
                    <tr
                      key={donor.id}
                      className="border-t border-[var(--hair)] transition-colors hover:bg-[var(--row-hover)]"
                    >
                      <td className="py-3.25 pr-2 pl-0">
                        <div className="flex items-center gap-2.5">
                          <span
                            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-[11px] font-bold text-white"
                            style={{ backgroundColor: getAvatarColor(donor.companyName) }}
                          >
                            {getInitials(donor.companyName)}
                          </span>
                          <Link
                            to={paths.donorDetail(donor.id)}
                            className="whitespace-nowrap text-[13.5px] font-semibold text-[var(--ink)] hover:text-[var(--brand)]"
                          >
                            {donor.companyName}
                          </Link>
                        </div>
                      </td>
                      <td className="py-3.25 pr-2 pl-0 whitespace-nowrap text-[13px] font-medium text-[var(--muted-c)]">
                        {formatDate(donor.followUpDate)}
                      </td>
                      <td className="py-3.25 pr-2 pl-0 whitespace-nowrap text-[13px] font-bold text-[var(--brand-red)]">
                        {`${days} ${days === 1 ? 'day' : 'days'}`}
                      </td>
                      <td className="py-3.25 pr-0 pl-2 text-right whitespace-nowrap">
                        <Link
                          to={paths.donorDetail(donor.id)}
                          className="border-b-[1.5px] border-[var(--brand-yellow)] pb-0.25 text-[13px] font-bold text-[var(--ink)] transition-colors hover:text-[var(--brand)]"
                        >
                          View donor
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            {totalCount > rows.length && (
              <div className="flex justify-end pt-2.5">
                <Link
                  to={`${paths.donors}?followUpBefore=${yesterdayYmd()}&sortBy=followUpDate&sortDir=asc`}
                  className="text-[12px] font-bold text-[var(--ink)] hover:text-[var(--brand)]"
                >
                  View all {totalCount} →
                </Link>
              </div>
            )}
          </div>
        )}
      </div>
    </section>
  );
}
