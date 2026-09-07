import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, TriangleAlert } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { useApprovalAction } from '../hooks';
import { rejectDonorSchema, REJECT_REASON_MIN, type RejectDonorFormValues } from '../schemas/rejectDonor.schema';
import type { ApprovalDto } from '../types';

interface RejectDonorDialogProps {
  approval: ApprovalDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function RejectDonorDialog({ approval, open, onOpenChange }: RejectDonorDialogProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const action = useApprovalAction();

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RejectDonorFormValues>({
    resolver: zodResolver(rejectDonorSchema),
    defaultValues: { rejectionReason: '' },
  });

  const reason = useWatch({ control, name: 'rejectionReason' }) ?? '';
  const trimmedLength = reason.trim().length;

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset({ rejectionReason: '' });
      setGeneralError(null);
      action.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (!approval) return;
    setGeneralError(null);
    try {
      await action.mutateAsync({
        approvalId: approval.id,
        action: 'reject',
        rejectionReason: values.rejectionReason.trim(),
      });
      handleOpenChange(false);
    } catch {
      setGeneralError('Could not reject this submission. Please try again.');
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <div className="flex items-start gap-3.5">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-[13px] bg-[#FBE9E9] text-[#B12A2D]">
              <TriangleAlert className="h-4.5 w-4.5" />
            </span>
            <div className="min-w-0 flex-1">
              <DialogTitle>Reject donor submission</DialogTitle>
              <DialogDescription>
                You are rejecting{' '}
                <span className="font-extrabold text-[var(--ink)]">{approval?.donor.companyName ?? 'this donor'}</span>.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-2">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          <label htmlFor="rejection-reason" className="text-xs font-bold text-foreground">
            Rejection reason <span aria-hidden className="text-destructive">*</span>
          </label>
          <textarea
            id="rejection-reason"
            rows={5}
            {...register('rejectionReason')}
            placeholder="Explain what is wrong with this submission and what the relationship manager should do next."
            className={`resize-none rounded-2xl border bg-field px-3.75 py-3.5 text-[13.5px] font-medium leading-relaxed text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
              errors.rejectionReason ? 'border-destructive/70' : 'border-border'
            }`}
          />
          <div className="flex items-start justify-between gap-3.5">
            <p className="m-0 max-w-[340px] text-[11.5px] font-medium leading-snug text-muted-foreground">
              This reason is sent to the relationship manager. Minimum {REJECT_REASON_MIN} characters.
            </p>
            <span
              className={`shrink-0 text-[11.5px] font-bold tabular-nums ${
                trimmedLength >= REJECT_REASON_MIN ? 'text-[#1E6E3C]' : 'text-[#B12A2D]'
              }`}
            >
              {trimmedLength} / {REJECT_REASON_MIN} min
            </span>
          </div>
          {errors.rejectionReason && (
            <span className="text-xs text-destructive">{errors.rejectionReason.message}</span>
          )}

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="destructive"
              size="sm"
              disabled={isSubmitting || trimmedLength < REJECT_REASON_MIN}
            >
              {isSubmitting ? 'Rejecting…' : 'Confirm rejection'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
