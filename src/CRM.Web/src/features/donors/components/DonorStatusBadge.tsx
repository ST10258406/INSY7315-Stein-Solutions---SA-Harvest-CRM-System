const STATUS_STYLES: Record<string, string> = {
  Active: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30',
  PendingReview: 'bg-amber-500/15 text-amber-400 border-amber-500/30',
  Lapsed: 'bg-[#6B6B60]/15 text-[#B9B9AE] border-[#2B2B23]',
  Rejected: 'bg-rose-500/15 text-rose-400 border-rose-500/30',
};

const STATUS_LABELS: Record<string, string> = {
  Active: 'Active',
  PendingReview: 'Pending review',
  Lapsed: 'Lapsed',
  Rejected: 'Rejected',
};

export function DonorStatusBadge({ status }: { status: string }) {
  const style = STATUS_STYLES[status] ?? 'bg-[#26261D] text-[#B9B9AE] border-[#2B2B23]';
  const label = STATUS_LABELS[status] ?? status;

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium whitespace-nowrap ${style}`}
    >
      {label}
    </span>
  );
}
