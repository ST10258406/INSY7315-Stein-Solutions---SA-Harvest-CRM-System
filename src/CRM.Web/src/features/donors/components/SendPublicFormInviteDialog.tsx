import { useState } from 'react';
import { useForm } from 'react-hook-form';
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
import { useSendPublicFormInvite } from '../hooks';
import { applyServerErrors } from '../lib/applyServerErrors';
import {
  sendPublicFormInviteFormSchema,
  defaultSendPublicFormInviteValues,
  formValuesToSendPublicFormInviteRequest,
  type SendPublicFormInviteFormValues,
} from '../schemas';

interface SendPublicFormInviteDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function SendPublicFormInviteDialog({ open, onOpenChange }: SendPublicFormInviteDialogProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const sendInvite = useSendPublicFormInvite();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<SendPublicFormInviteFormValues>({
    resolver: zodResolver(sendPublicFormInviteFormSchema),
    defaultValues: defaultSendPublicFormInviteValues,
  });

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset(defaultSendPublicFormInviteValues);
      setGeneralError(null);
      sendInvite.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setGeneralError(null);
    try {
      await sendInvite.mutateAsync(formValuesToSendPublicFormInviteRequest(values));
      handleOpenChange(false);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) {
        setGeneralError('Could not send this invitation. Please try again.');
      }
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Send public form</DialogTitle>
          <DialogDescription>
            Email a prospective donor a link to the public onboarding form.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-3">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          <div className="flex flex-col gap-2">
            <label htmlFor="invite-to" className="text-xs font-bold text-foreground">
              To
            </label>
            <input
              id="invite-to"
              type="email"
              {...register('to')}
              placeholder="prospect@example.com"
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.to ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.to && <span className="text-xs text-destructive">{errors.to.message}</span>}
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="invite-subject" className="text-xs font-bold text-foreground">
              Subject
            </label>
            <input
              id="invite-subject"
              {...register('subject')}
              placeholder="Subject"
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.subject ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.subject && <span className="text-xs text-destructive">{errors.subject.message}</span>}
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="invite-body" className="text-xs font-bold text-foreground">
              Message
            </label>
            <textarea
              id="invite-body"
              rows={6}
              {...register('body')}
              placeholder="Write your message — plain text only, no formatting."
              className={`resize-y rounded-2xl border bg-field px-3.75 py-3.5 text-[13.5px] font-medium leading-relaxed text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.body ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.body ? (
              <span className="text-xs text-destructive">{errors.body.message}</span>
            ) : (
              <span className="text-[11.5px] font-medium text-muted-foreground">
                The registration form link is added automatically — no need to paste it yourself.
              </span>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Sending…' : 'Send invitation'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
