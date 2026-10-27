import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, CheckCircle2, TriangleAlert, Undo2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { paths } from '@/routes/paths';
import { useVettingDecision } from '../hooks';
import { DECISION_REASON_MAX, vettingDecisionSchema, type VettingDecisionFormValues } from '../schemas/vettingDecision.schema';
import type { VettingDecision, VettingSubmission } from '../types';

const COPY: Record<VettingDecision, { title: string; help: string; reasonLabel: string; confirm: string; done: string }> = {
  Approve: {
    title: 'Approve this vetting',
    help: 'The donor passes vetting. A reason is optional.',
    reasonLabel: 'Note (optional)',
    confirm: 'Approve',
    done: 'approved',
  },
  Return: {
    title: 'Return to officer',
    help: 'The officer will see this reason and can correct and resubmit.',
    reasonLabel: 'What needs correcting',
    confirm: 'Return to officer',
    done: 'returned to the officer',
  },
  Decline: {
    title: 'Decline this vetting',
    help: 'This closes the vetting. The reason is kept with the submission.',
    reasonLabel: 'Reason for declining',
    confirm: 'Decline',
    done: 'declined',
  },
};

interface VettingDecisionDialogProps {
  submission: VettingSubmission;
  decision: VettingDecision | null;
  onOpenChange: (open: boolean) => void;
}

/** Approve / Return / Decline, modelled on RejectDonorDialog. Return and Decline need a reason. */
export function VettingDecisionDialog({ submission, decision, onOpenChange }: VettingDecisionDialogProps) {
  const navigate = useNavigate();
  const [generalError, setGeneralError] = useState<string | null>(null);
  const decide = useVettingDecision(submission.id);
  const copy = COPY[decision ?? 'Approve'];
  const required = decision !== 'Approve';

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<VettingDecisionFormValues>({
    resolver: zodResolver(vettingDecisionSchema),
    values: { decision: decision ?? 'Approve', reason: '' },
  });

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset();
      setGeneralError(null);
      decide.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async ({ decision: chosen, reason }) => {
    setGeneralError(null);
    try {
      await decide.mutateAsync({ decision: chosen, ...(reason ? { reason } : {}) });
      toast.success(`${submission.formReference} ${COPY[chosen].done}.`);
      handleOpenChange(false);
      navigate(paths.vettingReview);
    } catch (error) {
      const status = (error as { response?: { status?: number } }).response?.status;
      // 409: another admin decided first; the hook has already refetched the submission.
      setGeneralError(status === 409 ? 'This submission was already reviewed.' : 'Could not record this decision. Please try again.');
    }
  });

  const Icon = decision === 'Approve' ? CheckCircle2 : decision === 'Return' ? Undo2 : TriangleAlert;

  return (
    <Dialog open={decision !== null} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        <DialogHeader>
          <div className="flex items-start gap-3.5">
            <span
              className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-[13px] ${
                decision === 'Approve' ? 'bg-emerald-500/15 text-emerald-700' : 'bg-[#FBE9E9] text-[#B12A2D]'
              }`}
            >
              <Icon className="h-4.5 w-4.5" />
            </span>
            <div className="min-w-0 flex-1">
              <DialogTitle>{copy.title}</DialogTitle>
              <DialogDescription>
                <span className="font-extrabold text-[var(--ink)]">{submission.formReference}</span> for {submission.donorCompanyName}.{' '}
                {copy.help}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-2">
          {generalError && (
            <div role="alert" className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}
          <label htmlFor="decision-reason" className="text-xs font-bold text-foreground">
            {copy.reasonLabel} {required && <span aria-hidden className="text-destructive">*</span>}
          </label>
          <textarea
            id="decision-reason"
            rows={5}
            maxLength={DECISION_REASON_MAX}
            {...register('reason')}
            className={`resize-none rounded-2xl border bg-field px-3.75 py-3.5 text-[13.5px] font-medium leading-relaxed text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
              errors.reason ? 'border-destructive/70' : 'border-border'
            }`}
          />
          {errors.reason && <span className="text-xs text-destructive">{errors.reason.message}</span>}
          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" variant={decision === 'Approve' ? 'approve' : 'destructive'} size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : copy.confirm}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
