import type { SiteVisitStatus, VettingApprovalState } from '../types';
import { APPROVAL_STATE_LABELS, SITE_VISIT_STATUS_LABELS } from '../lib/vettingLabels';

const AMBER = { pill: 'bg-amber-500/15 text-amber-700 dark:text-amber-400', dot: 'bg-amber-500' };
const SKY = { pill: 'bg-sky-500/15 text-sky-700 dark:text-sky-400', dot: 'bg-sky-500' };
const GREEN = { pill: 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-400', dot: 'bg-emerald-500' };
const ROSE = { pill: 'bg-rose-500/15 text-rose-700 dark:text-rose-400', dot: 'bg-rose-500' };
const MUTED = { pill: 'bg-muted-foreground/15 text-muted-foreground', dot: 'bg-muted-foreground' };

const APPROVAL_STYLES: Record<VettingApprovalState, typeof MUTED> = {
  New: MUTED,
  Ready: SKY,
  Returned: AMBER,
  Approved: GREEN,
  Declined: ROSE,
};

const VISIT_STYLES: Record<SiteVisitStatus, typeof MUTED> = {
  ToDo: MUTED,
  InProgress: AMBER,
  Completed: GREEN,
};

type VettingStatusBadgeProps =
  | { kind: 'approval'; status: VettingApprovalState }
  | { kind: 'visit'; status: SiteVisitStatus };

/** Submission approval state or site-visit status, styled like DonorStatusBadge. */
export function VettingStatusBadge(props: VettingStatusBadgeProps) {
  const style = (props.kind === 'approval' ? APPROVAL_STYLES[props.status] : VISIT_STYLES[props.status]) ?? MUTED;
  const label =
    (props.kind === 'approval' ? APPROVAL_STATE_LABELS[props.status] : SITE_VISIT_STATUS_LABELS[props.status]) ?? props.status;

  return (
    <span
      className={`inline-flex items-center gap-1.75 rounded-2xl px-3 py-1 text-[12.5px] font-bold whitespace-nowrap ${style.pill}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${style.dot}`} />
      <span>{label}</span>
    </span>
  );
}
