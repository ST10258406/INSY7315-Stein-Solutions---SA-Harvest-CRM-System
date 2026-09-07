import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Check, X, FileText, Globe } from 'lucide-react';
import { paths } from '@/routes/paths';
import { Button } from '@/components/ui/button';
import { DonorStatusBadge } from '@/features/donors/components/DonorStatusBadge';
import { getInitials, getAvatarColor } from '@/features/donors/lib/avatar';
import { formatRelativeTime } from '@/features/interactions/lib/interactionMeta';
import { useApprovalAction } from '../hooks';
import type { ApprovalDto } from '../types';

interface ApprovalRowProps {
  approval: ApprovalDto;
  onReject: (approval: ApprovalDto) => void;
}

export function ApprovalRow({ approval, onReject }: ApprovalRowProps) {
  const [confirming, setConfirming] = useState(false);
  const action = useApprovalAction();

  const isPending = approval.status === 'Pending';
  const isApproved = approval.status === 'Approved';
  const requester = approval.requestedBy?.fullName ?? null;
  const reviewer = approval.reviewedBy?.fullName ?? '—';
  const busy = action.isPending;

  const approve = () => {
    action.mutate(
      { approvalId: approval.id, action: 'approve' },
      { onSettled: () => setConfirming(false) },
    );
  };

  return (
    <article
      className={`rounded-2xl border border-[var(--border)] bg-[var(--card)] p-[20px_22px] shadow-[0_1px_3px_var(--shadow)] ${
        isPending ? 'border-l-[3px] border-l-[#D99A00]' : ''
      }`}
    >
      <div className="flex flex-wrap items-center gap-5">
        <div className="min-w-[220px] flex-1">
          <Link
            to={paths.donorDetail(approval.donor.id)}
            className="text-[15.5px] font-extrabold tracking-tight text-[var(--ink)] hover:text-[var(--brand)]"
          >
            {approval.donor.companyName}
          </Link>
          <div className="mt-1.75 flex flex-wrap items-center gap-2.5">
            <DonorStatusBadge status={approval.donor.status} />
            <span className="inline-flex items-center gap-1.5 text-[12px] font-medium text-[var(--muted-c)]">
              {requester ? <FileText className="h-3.25 w-3.25" /> : <Globe className="h-3.25 w-3.25" />}
              {requester ? 'Internal capture' : 'Public form'}
            </span>
          </div>
        </div>

        <div className="min-w-[164px] shrink-0">
          <div className="mb-1.75 text-[10.5px] font-bold tracking-wide text-[var(--muted2)]">REQUESTED BY</div>
          <div className="flex items-center gap-2.25">
            <span
              className="flex h-7 w-7 items-center justify-center rounded-full text-[10px] font-bold text-white"
              style={{ backgroundColor: getAvatarColor(requester) }}
            >
              {requester ? getInitials(requester) : '—'}
            </span>
            <span className="text-[13px] font-semibold text-[var(--ink)]">{requester ?? 'External submission'}</span>
          </div>
        </div>

        <div className="min-w-[110px] shrink-0">
          <div className="mb-1.75 text-[10.5px] font-bold tracking-wide text-[var(--muted2)]">SUBMITTED</div>
          <div className="text-[13px] font-semibold text-[var(--ink)]">{formatRelativeTime(approval.createdAt)}</div>
        </div>

        <div className="relative ml-auto flex items-center gap-2.5">
          {isPending ? (
            <>
              <Button variant="approve" size="sm" disabled={busy} onClick={() => setConfirming((v) => !v)}>
                <Check className="h-3.75 w-3.75 stroke-[2.4]" />
                <span>Approve</span>
              </Button>
              <Button variant="reject" size="sm" disabled={busy} onClick={() => onReject(approval)}>
                <X className="h-3.75 w-3.75 stroke-[2.2]" />
                <span>Reject</span>
              </Button>

              {confirming && (
                <>
                  <div className="fixed inset-0 z-30" onClick={() => setConfirming(false)} aria-hidden />
                  <div className="absolute right-0 top-[52px] z-40 w-[268px] rounded-2xl border border-[var(--border)] bg-[var(--card)] p-4 text-left shadow-[0_16px_40px_rgba(20,20,15,0.2)]">
                    <div className="text-[13.5px] font-extrabold tracking-tight text-[var(--ink)]">
                      Approve this donor?
                    </div>
                    <p className="m-0 mb-3.5 mt-1.5 text-[12px] font-medium leading-normal text-[var(--muted-c)]">
                      {approval.donor.companyName} goes live as an Active donor and the relationship manager is notified.
                    </p>
                    <div className="flex items-center justify-end gap-2">
                      <Button variant="secondary" size="sm" className="h-9" onClick={() => setConfirming(false)}>
                        Cancel
                      </Button>
                      <Button variant="approve" size="sm" className="h-9" disabled={busy} onClick={approve}>
                        {busy ? 'Approving…' : 'Confirm'}
                      </Button>
                    </div>
                  </div>
                </>
              )}
            </>
          ) : (
            <span
              className={`inline-flex items-center gap-2 rounded-2xl px-3.5 py-2 text-[12.5px] font-bold ${
                isApproved ? 'bg-[#E5F4E9] text-[#1E6E3C]' : 'bg-[#FBE9E9] text-[#9B2C2C]'
              }`}
            >
              {isApproved ? <Check className="h-3.5 w-3.5 stroke-[2.6]" /> : <X className="h-3.5 w-3.5 stroke-[2.4]" />}
              <span>
                {approval.status} by {reviewer}
                {approval.reviewedAt ? ` · ${formatRelativeTime(approval.reviewedAt)}` : ''}
              </span>
            </span>
          )}
        </div>
      </div>

      {!isPending && !isApproved && approval.rejectionReason && (
        <div className="mt-4 rounded-2xl border border-[#F1CFCF] bg-[#FBE9E9] p-[14px_16px]">
          <div className="mb-1.5 text-[10.5px] font-bold tracking-wide text-[#9B2C2C]">REJECTION REASON</div>
          <p className="m-0 text-[13px] font-medium leading-relaxed text-[#7C2325] [text-wrap:pretty]">
            {approval.rejectionReason}
          </p>
        </div>
      )}
    </article>
  );
}
