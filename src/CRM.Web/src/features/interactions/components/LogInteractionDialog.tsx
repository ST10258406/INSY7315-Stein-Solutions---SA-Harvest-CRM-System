import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import { useLogInteraction } from '../hooks';
import { interactionTypeLabel } from '../lib/interactionMeta';
import { InteractionTypeIcon } from './InteractionTypeIcon';
import { LOGGABLE_INTERACTION_TYPES } from '../types';
import {
  logInteractionFormSchema,
  emptyLogInteractionValues,
  formValuesToLogInteractionRequest,
  type LogInteractionFormValues,
} from '../schemas/logInteraction.schema';

interface LogInteractionDialogProps {
  donorId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** Tomorrow, as yyyy-MM-dd — the earliest date the backend accepts for a follow-up. */
function minFollowUpDate(): string {
  return new Date(Date.now() + 86_400_000).toISOString().slice(0, 10);
}

export function LogInteractionDialog({ donorId, open, onOpenChange }: LogInteractionDialogProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const logInteraction = useLogInteraction(donorId);

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LogInteractionFormValues>({
    resolver: zodResolver(logInteractionFormSchema),
    defaultValues: emptyLogInteractionValues,
  });

  // Clear the form + any stale errors on the way out, so the next open starts fresh.
  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset(emptyLogInteractionValues);
      setGeneralError(null);
      logInteraction.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setGeneralError(null);
    try {
      await logInteraction.mutateAsync(formValuesToLogInteractionRequest(values));
      handleOpenChange(false);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) {
        setGeneralError('Could not save this interaction. Please try again.');
      }
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Log interaction</DialogTitle>
          <DialogDescription>Record a call, email, meeting, or note against this donor.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-3">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          <Controller
            control={control}
            name="interactionType"
            render={({ field }) => (
              <div className="flex flex-col gap-2">
                <span className="text-xs font-bold text-foreground">Type</span>
                <div className="flex flex-wrap items-center gap-2">
                  {LOGGABLE_INTERACTION_TYPES.map((type) => {
                    const selected = field.value === type;
                    return (
                      <button
                        type="button"
                        key={type}
                        onClick={() => field.onChange(type)}
                        aria-pressed={selected}
                        className={`flex h-8.5 items-center gap-1.75 rounded-full border px-3.25 text-xs font-bold transition-colors ${
                          selected
                            ? 'border-[var(--brand-yellow)] bg-[var(--brand-yellow)] text-[#16160F]'
                            : 'border-[var(--border)] bg-[var(--field)] text-[var(--ink)] hover:border-[var(--ink)]'
                        }`}
                      >
                        <InteractionTypeIcon type={type} className="h-3.5 w-3.5" />
                        <span>{interactionTypeLabel(type)}</span>
                      </button>
                    );
                  })}
                </div>
                {errors.interactionType && (
                  <span className="text-xs text-destructive">{errors.interactionType.message}</span>
                )}
              </div>
            )}
          />

          <div className="flex flex-col gap-2">
            <label htmlFor="interaction-subject" className="text-xs font-bold text-foreground">
              Subject
            </label>
            <input
              id="interaction-subject"
              {...register('subject')}
              placeholder="Subject"
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.subject ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.subject && <span className="text-xs text-destructive">{errors.subject.message}</span>}
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="interaction-body" className="text-xs font-bold text-foreground">
              What happened? <span aria-hidden className="text-destructive">*</span>
            </label>
            <textarea
              id="interaction-body"
              rows={4}
              {...register('body')}
              placeholder="What happened?"
              className={`resize-y rounded-2xl border bg-field px-3.75 py-3.5 text-[13.5px] font-medium leading-relaxed text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.body ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.body && <span className="text-xs text-destructive">{errors.body.message}</span>}
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="interaction-followup" className="text-xs font-bold text-foreground">
              Follow-up date <span className="font-medium text-muted-foreground">(optional)</span>
            </label>
            <input
              id="interaction-followup"
              type="date"
              min={minFollowUpDate()}
              {...register('followUpDate')}
              className={`h-11 w-full rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.followUpDate ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.followUpDate ? (
              <span className="text-xs text-destructive">{errors.followUpDate.message}</span>
            ) : (
              <span className="text-[11.5px] font-medium text-muted-foreground">
                Setting a date updates this donor's follow-up reminder.
              </span>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save interaction'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
