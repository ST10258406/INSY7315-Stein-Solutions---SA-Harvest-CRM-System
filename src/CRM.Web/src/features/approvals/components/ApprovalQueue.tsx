import { CheckCircle2, TriangleAlert } from 'lucide-react';
import { ApprovalRow } from './ApprovalRow';
import type { ApiError, ApprovalDto, ApprovalStatus } from '../types';

interface ApprovalQueueProps {
  approvals: ApprovalDto[];
  isPending: boolean;
  isError: boolean;
  error?: ApiError | null;
  onRetry: () => void;
  onReject: (approval: ApprovalDto) => void;
  status: ApprovalStatus;
}

const EMPTY_COPY: Record<ApprovalStatus, { title: string; description: string }> = {
  Pending: {
    title: "No pending approvals — you're all caught up!",
    description: 'New donor submissions appear here the moment they arrive.',
  },
  Approved: { title: 'Nothing approved yet', description: 'Approved submissions will be listed here.' },
  Rejected: { title: 'Nothing rejected yet', description: 'Rejected submissions will be listed here.' },
};

export function ApprovalQueue({ approvals, isPending, isError, error, onRetry, onReject, status }: ApprovalQueueProps) {
  if (isPending) {
    return (
      <div className="flex flex-col gap-2.5" aria-hidden>
        {Array.from({ length: 3 }).map((_, index) => (
          <div key={index} className="h-[104px] animate-pulse rounded-2xl bg-muted" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-2xl border border-[var(--border)] bg-[var(--card)] py-12 text-center">
        <TriangleAlert className="h-5 w-5 text-muted-foreground" />
        <p className="m-0 text-sm font-semibold text-foreground">Couldn't load the approval queue</p>
        <p className="m-0 text-xs text-muted-foreground">{error?.response?.data?.message ?? 'Something went wrong.'}</p>
        <button
          type="button"
          onClick={onRetry}
          className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
        >
          Retry
        </button>
      </div>
    );
  }

  if (approvals.length === 0) {
    const copy = EMPTY_COPY[status];
    return (
      <div className="flex flex-col items-center gap-3 rounded-2xl border border-[var(--border)] bg-[var(--card)] py-16 text-center shadow-[0_1px_3px_var(--shadow)]">
        <span className="flex h-14 w-14 items-center justify-center rounded-[20px] bg-[#E5F4E9] text-[#1E6E3C]">
          <CheckCircle2 className="h-6 w-6" />
        </span>
        <h3 className="m-0 text-[19px] font-extrabold tracking-tight text-[var(--ink)]">{copy.title}</h3>
        <p className="m-0 max-w-[380px] text-[13.5px] font-medium text-[var(--muted-c)]">{copy.description}</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-2.5">
      {approvals.map((approval) => (
        <ApprovalRow key={approval.id} approval={approval} onReject={onReject} />
      ))}
    </div>
  );
}
