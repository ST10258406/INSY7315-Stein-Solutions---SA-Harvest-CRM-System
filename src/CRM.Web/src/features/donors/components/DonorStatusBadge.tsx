const STATUS_STYLES: Record<string, { pill: string; dot: string }> = {
  Active: { pill: 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-400', dot: 'bg-emerald-500' },
  PendingReview: { pill: 'bg-amber-500/15 text-amber-700 dark:text-amber-400', dot: 'bg-amber-500' },
  Lapsed: { pill: 'bg-rose-500/15 text-rose-700 dark:text-rose-400', dot: 'bg-rose-500' },
  Rejected: { pill: 'bg-muted-foreground/15 text-muted-foreground', dot: 'bg-muted-foreground' },
};

const STATUS_LABELS: Record<string, string> = {
  Active: 'Active',
  PendingReview: 'Pending review',
  Lapsed: 'Lapsed',
  Rejected: 'Rejected',
};

export function DonorStatusBadge({ status }: { status: string }) {
  const style = STATUS_STYLES[status] ?? { pill: 'bg-muted text-muted-foreground', dot: 'bg-muted-foreground' };
  const label = STATUS_LABELS[status] ?? status;

  return (
    <span
      className={`inline-flex items-center gap-1.75 rounded-2xl px-3 py-1 text-[12.5px] font-bold whitespace-nowrap ${style.pill}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${style.dot}`} />
      <span>{label}</span>
    </span>
  );
}
