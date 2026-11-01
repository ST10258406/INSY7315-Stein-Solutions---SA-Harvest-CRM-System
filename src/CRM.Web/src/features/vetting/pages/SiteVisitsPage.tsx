import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDown, ClipboardCheck, Plus } from 'lucide-react';
import { toast } from 'sonner';
import { DatePicker } from '@/components/common/DatePicker';
import { Button } from '@/components/ui/button';
import { taskMinDueDate as todayYmd } from '@/features/tasks/schemas/taskForm.schema';
import { paths } from '@/routes/paths';
import { useOfficers, useSiteVisits, useUpdateSiteVisit } from '../hooks';
import { SiteVisitFormDialog } from '../components/SiteVisitFormDialog';
import { TD, TH, VettingTableStates } from '../components/VettingTableStates';
import { SITE_VISIT_STATUS_LABELS } from '../lib/vettingLabels';
import type { SiteVisitStatus, VettingSiteVisit } from '../types';

const SELECT =
  'h-10 appearance-none rounded-full border border-border bg-field pl-3.75 pr-9 text-[13px] font-semibold text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60';

function startVettingHref(visit: VettingSiteVisit) {
  const query = new URLSearchParams({ donorId: visit.donorId, officerId: visit.officerId, siteVisitId: visit.id });
  return `${paths.vettingNew}?${query}`;
}

/** /vetting/visits — one day's site visits, with inline status changes. */
export default function SiteVisitsPage() {
  const [date, setDate] = useState(todayYmd);
  const [officerId, setOfficerId] = useState('');
  const [scheduling, setScheduling] = useState(false);

  const { data: officers = [] } = useOfficers();
  const { data: visits = [], isPending, isError, error, refetch } = useSiteVisits(date, officerId || undefined);
  const updateVisit = useUpdateSiteVisit();

  const changeStatus = (visit: VettingSiteVisit, visitStatus: SiteVisitStatus) =>
    updateVisit.mutate(
      { id: visit.id, request: { visitStatus } },
      {
        onSuccess: () => toast.success(`${visit.donorCompanyName}: ${SITE_VISIT_STATUS_LABELS[visitStatus]}`),
        onError: (err) => toast.error(err.response?.data?.message ?? 'Could not update this visit.'),
      },
    );

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_40px]">
      <div className="mb-5 flex flex-wrap items-end gap-6">
        <div>
          <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Site visits</h1>
          <p className="m-0 text-sm font-medium text-[var(--muted-c)]">Each day's vetting visits, in time order.</p>
        </div>
        <div className="ml-auto">
          <Button size="sm" onClick={() => setScheduling(true)}>
            <Plus className="h-4 w-4 stroke-[2.2]" />
            <span>Schedule visit</span>
          </Button>
        </div>
      </div>

      <section className="mb-4.5 flex flex-wrap items-center gap-3 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4">
        <DatePicker variant="pill" value={date} onChange={(next) => setDate(next || todayYmd())} aria-label="Visit date" />
        <div className="relative">
          <select aria-label="Officer" value={officerId} onChange={(e) => setOfficerId(e.target.value)} className={SELECT}>
            <option value="">All officers</option>
            {officers.map((o) => (
              <option key={o.id} value={o.id}>
                {o.fullName}
              </option>
            ))}
          </select>
          <ChevronDown className="pointer-events-none absolute top-3.25 right-3.5 h-3.5 w-3.5 text-muted-foreground" />
        </div>
      </section>

      <div className="overflow-x-auto rounded-xl bg-[var(--card)] shadow-[0_1px_3px_var(--shadow)]">
        <table className="w-full min-w-[920px] border-collapse">
          <thead>
            <tr className="bg-[var(--hair)]">
              <th className={TH}>Time</th>
              <th className={TH}>Donor</th>
              <th className={TH}>Location</th>
              <th className={TH}>Officer</th>
              <th className={TH}>Notes</th>
              <th className={TH}>Status</th>
              <th className={TH}>
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>
          <tbody>
            <VettingTableStates
              colSpan={7}
              isPending={isPending}
              isError={isError}
              error={error}
              onRetry={refetch}
              isEmpty={visits.length === 0}
              emptyText="No visits scheduled for this day."
            />
            {visits.map((visit) => (
              <tr key={visit.id}>
                <td className={`${TD} font-bold tabular-nums`}>{visit.scheduledTime.slice(0, 5)}</td>
                <td className={TD}>
                  <Link to={paths.donorDetail(visit.donorId)} className="font-semibold hover:underline">
                    {visit.donorCompanyName}
                  </Link>
                </td>
                <td className={TD}>{visit.locationLabel}</td>
                <td className={TD}>{visit.officerInitials}</td>
                <td className={`${TD} max-w-[220px] truncate text-muted-foreground`} title={visit.visitNotes ?? undefined}>
                  {visit.visitNotes ?? '—'}
                </td>
                <td className={TD}>
                  <div className="relative inline-block">
                    <select
                      aria-label={`Status for ${visit.donorCompanyName}`}
                      value={visit.visitStatus}
                      onChange={(e) => changeStatus(visit, e.target.value as SiteVisitStatus)}
                      className={SELECT}
                    >
                      {Object.entries(SITE_VISIT_STATUS_LABELS).map(([value, label]) => (
                        <option key={value} value={value}>
                          {label}
                        </option>
                      ))}
                    </select>
                    <ChevronDown className="pointer-events-none absolute top-3.25 right-3.5 h-3.5 w-3.5 text-muted-foreground" />
                  </div>
                </td>
                <td className={`${TD} text-right`}>
                  {visit.visitStatus !== 'Completed' && (
                    <Link
                      to={startVettingHref(visit)}
                      className="inline-flex h-9 items-center gap-1.5 rounded-full border border-border px-3.5 text-[12.5px] font-bold whitespace-nowrap hover:border-foreground"
                    >
                      <ClipboardCheck className="h-3.5 w-3.5" />
                      Start vetting
                    </Link>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {scheduling && (
        <SiteVisitFormDialog
          key={date}
          open
          onOpenChange={(open) => !open && setScheduling(false)}
          defaultDate={date}
          defaultOfficerId={officerId || undefined}
        />
      )}
    </main>
  );
}
