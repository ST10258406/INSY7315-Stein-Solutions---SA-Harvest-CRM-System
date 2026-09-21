import { useEffect, useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, Mail, Send, TriangleAlert, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '@/components/ui/sheet';
import { getInitials, getAvatarColor } from '@/features/donors/lib/avatar';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import { useInteractions, useSendDonorEmail } from '../hooks';
import { formatRelativeTime } from '../lib/interactionMeta';
import {
  sendDonorEmailFormSchema,
  formValuesToSendDonorEmailRequest,
  type SendDonorEmailFormValues,
} from '../schemas/sendDonorEmail.schema';
import type { InteractionLogDto } from '../types';

interface EmailPanelProps {
  donorId: string;
  donorName: string;
  contactName: string;
  /** Donor's primary contact email — pre-fills "To", editable. */
  defaultTo: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

function EmailThreadCard({ interaction }: { interaction: InteractionLogDto }) {
  const sender = interaction.createdBy?.fullName ?? 'System';

  return (
    <article className="rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4.5 py-4 shadow-[0_1px_3px_var(--shadow)]">
      <div className="flex items-center gap-2.5">
        <span
          className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full text-xs font-bold text-white"
          style={{ backgroundColor: getAvatarColor(sender) }}
        >
          {getInitials(sender)}
        </span>
        <div className="min-w-0 leading-tight">
          <div className="flex flex-wrap items-center gap-1.75">
            <span className="text-[13.5px] font-bold text-[var(--ink)]">{sender}</span>
            <Mail className="h-3.25 w-3.25 text-[var(--muted2)]" />
          </div>
        </div>
        <span className="ml-auto flex-shrink-0 text-[11.5px] font-semibold text-[var(--muted2)]">
          {formatRelativeTime(interaction.createdAt)}
        </span>
      </div>

      <div className="mt-3">
        {interaction.subject && (
          <div className="mb-1.5 text-[13.5px] font-bold text-[var(--ink)]">{interaction.subject}</div>
        )}
        <div className="whitespace-pre-line text-[13px] font-medium leading-relaxed text-[var(--ink)]">
          {interaction.body}
        </div>
      </div>
    </article>
  );
}

export function EmailPanel({ donorId, donorName, contactName, defaultTo, open, onOpenChange }: EmailPanelProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [editingTo, setEditingTo] = useState(!defaultTo);
  const sendEmail = useSendDonorEmail(donorId);
  const { data, isPending, isError } = useInteractions(donorId, { interactionType: 'Email' }, { enabled: open });

  // Newest-first from the API — a thread reads top-to-bottom, oldest first.
  const emails = [...(data?.pages[0]?.data ?? [])].reverse();

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<SendDonorEmailFormValues>({
    resolver: zodResolver(sendDonorEmailFormSchema),
    defaultValues: { to: defaultTo, subject: '', body: '' },
  });

  const toValue = watch('to');

  // The panel is created once per donor page and toggled open/closed, so the
  // pre-filled "To" needs to (re)apply whenever it opens — not just on mount.
  useEffect(() => {
    if (open) {
      reset({ to: defaultTo, subject: '', body: '' });
      setEditingTo(!defaultTo);
      setGeneralError(null);
    }
  }, [open, defaultTo, reset]);

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset({ to: defaultTo, subject: '', body: '' });
      setEditingTo(!defaultTo);
      setGeneralError(null);
      sendEmail.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setGeneralError(null);
    try {
      await sendEmail.mutateAsync(formValuesToSendDonorEmailRequest(values));
      handleOpenChange(false);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) {
        setGeneralError('Could not send this email. Please try again.');
      }
    }
  });

  return (
    <Sheet open={open} onOpenChange={handleOpenChange}>
      <SheetContent className="p-0">
        <SheetHeader>
          <SheetTitle>
            {donorName} <span className="text-[var(--muted2)] font-semibold">—</span> Email Thread
          </SheetTitle>
          <SheetDescription>
            {contactName} · {defaultTo || 'No contact email on file'}
          </SheetDescription>
        </SheetHeader>

        <div className="flex min-h-0 flex-1 flex-col gap-3.5 overflow-y-auto bg-[var(--soft)] px-6.5 py-5.5">
          {isPending ? (
            <div className="flex flex-col gap-3" aria-hidden>
              {Array.from({ length: 2 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-2xl bg-muted" />
              ))}
            </div>
          ) : isError ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-2 text-center">
              <TriangleAlert className="h-5 w-5 text-[var(--muted2)]" />
              <p className="m-0 text-sm font-semibold text-[var(--ink)]">Couldn't load this donor's emails</p>
            </div>
          ) : emails.length === 0 ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-3 py-10 text-center">
              <span className="flex h-14 w-14 items-center justify-center rounded-full border border-[var(--border)] bg-[var(--card)]">
                <Mail className="h-5.5 w-5.5 text-[var(--muted2)]" />
              </span>
              <div className="leading-relaxed">
                <p className="m-0 text-[14.5px] font-bold text-[var(--ink)]">No emails yet</p>
                <p className="m-0 text-[12.5px] font-medium text-[var(--muted-c)]">Start the conversation below.</p>
              </div>
            </div>
          ) : (
            emails.map((email) => <EmailThreadCard key={email.id} interaction={email} />)
          )}
        </div>

        <form onSubmit={onSubmit} className="flex-shrink-0 border-t border-[var(--border)] bg-[var(--card)] px-6.5 pt-4 pb-4.5">
          {generalError && (
            <div className="mb-3 flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          <div className="flex flex-col gap-1.5 border-b border-[var(--hair)] pb-2.5">
            <div className="flex items-center gap-2.5">
              <span className="flex-shrink-0 text-xs font-bold text-[var(--muted2)]">To:</span>
              <Controller
                control={control}
                name="to"
                render={({ field }) =>
                  editingTo ? (
                    <input
                      {...field}
                      type="email"
                      aria-label="To"
                      autoFocus
                      placeholder="Add recipient email"
                      onBlur={() => {
                        field.onBlur();
                        if (field.value.trim()) setEditingTo(false);
                      }}
                      className="min-w-20 flex-1 border-none bg-transparent py-1 text-[12.5px] font-medium text-[var(--ink)] outline-none placeholder:text-[var(--muted-c)]"
                    />
                  ) : (
                    <span className="inline-flex h-7.5 items-center gap-1.75 rounded-full bg-[var(--chip)] pl-1 pr-1.5 text-[12.5px] font-semibold text-[var(--ink)]">
                      <span
                        className="flex h-5.5 w-5.5 items-center justify-center rounded-full text-[9px] font-bold text-white"
                        style={{ backgroundColor: getAvatarColor(field.value) }}
                      >
                        {field.value.charAt(0).toUpperCase()}
                      </span>
                      <span>{field.value}</span>
                      <button
                        type="button"
                        title="Change recipient"
                        onClick={() => setEditingTo(true)}
                        className="flex h-4.5 w-4.5 items-center justify-center rounded-full text-[var(--muted-c)] hover:bg-[var(--hover)]"
                      >
                        <X className="h-2.75 w-2.75" />
                      </button>
                    </span>
                  )
                }
              />
            </div>
            {errors.to && <span className="text-xs text-destructive">{errors.to.message}</span>}
          </div>

          <div className="flex flex-col gap-1.5 border-b border-[var(--hair)] py-2.5">
            <div className="flex items-center gap-2.5">
              <span className="flex-shrink-0 text-xs font-bold text-[var(--muted2)]">Subject:</span>
              <input
                {...register('subject')}
                aria-label="Subject"
                placeholder="Add a subject"
                className="flex-1 border-none bg-transparent py-0.5 text-[12.5px] font-semibold text-[var(--ink)] outline-none placeholder:font-medium placeholder:text-[var(--muted-c)]"
              />
            </div>
            {errors.subject && <span className="text-xs text-destructive">{errors.subject.message}</span>}
          </div>

          <div className="flex flex-col gap-1.5">
            <textarea
              {...register('body')}
              rows={4}
              aria-label="Message"
              placeholder="Write your message — plain text only, no formatting."
              className="resize-none border-none bg-transparent py-3 text-[13px] font-medium leading-relaxed text-[var(--ink)] outline-none placeholder:text-[var(--muted-c)]"
            />
            {errors.body && <span className="text-xs text-destructive">{errors.body.message}</span>}
          </div>

          <div className="mt-1.5 flex items-center justify-end">
            <Button type="submit" size="sm" disabled={isSubmitting || !toValue.trim()}>
              <Send className="h-3.5 w-3.5" />
              <span>{isSubmitting ? 'Sending…' : 'Send'}</span>
            </Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
}
