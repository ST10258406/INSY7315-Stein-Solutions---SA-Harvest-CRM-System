import { useState, type ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import { AlertCircle, ArrowLeft, Flag, Pencil, Send } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { DetailField } from '@/features/donors/components/DetailSectionCard';
import { paths } from '@/routes/paths';
import { useSubmitVetting, useVetting } from '../hooks';
import { AccordionFormLayout } from '../components/form/AccordionFormLayout';
import { FormAccordionSection } from '../components/form/FormAccordionSection';
import { AssessmentFileSlot } from '../components/AssessmentFileSlot';
import { EvidencePhotos, PhotosCapturedBadge } from '../components/EvidencePhotos';
import { VettingStatusBadge } from '../components/VettingStatusBadge';
import { FIELD_LABELS, FILE_SLOT_SECTION, SECTION_IDS, VETTING_SECTIONS } from '../lib/vettingSections';
import { FLAG_SOURCE_LABELS, PHOTO_CATEGORY_LABELS } from '../lib/vettingLabels';
import type { VettingFileSlot, VettingSubmission } from '../types';

const DATE = new Intl.DateTimeFormat('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });

function formatValue(value: unknown): ReactNode {
  if (value === null || value === undefined || value === '') return '—';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (Array.isArray(value))
    return value.length === 0 ? (
      'None'
    ) : (
      <span className="flex flex-wrap gap-1.5">
        {value.map((v) => (
          <span key={v} className="rounded-full border border-border bg-chip px-2.5 py-0.5 text-xs font-bold">
            {v}
          </span>
        ))}
      </span>
    );
  return String(value);
}

/** /vetting/:id — read-only view of a submission, with uploads and Submit for review while it is with the officer. */
export default function VettingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: submission, isPending, isError, error, refetch } = useVetting(id);

  if (isPending) return <div className="m-8 h-96 animate-pulse rounded-2xl bg-muted" aria-hidden />;
  if (isError || !submission)
    return (
      <div className="flex flex-col items-center gap-3 p-16 text-center">
        <p className="text-sm font-medium text-foreground">Couldn't load this vetting submission</p>
        <p className="text-xs text-muted-foreground">{error?.response?.data?.message ?? 'Something went wrong.'}</p>
        <Button variant="outline" size="sm" onClick={() => refetch()}>
          Retry
        </Button>
      </div>
    );

  return <SubmissionView submission={submission} />;
}

function SubmissionView({ submission }: { submission: VettingSubmission }) {
  const editable = submission.approvalState === 'New' || submission.approvalState === 'Returned';
  const fileSlots = (sectionId: string) =>
    (Object.entries(FILE_SLOT_SECTION) as [VettingFileSlot, string][]).filter(([, s]) => s === sectionId).map(([slot]) => slot);

  return (
    <div className="flex flex-col gap-6 p-8">
      <div>
        <Link to={paths.vetting} className="flex w-fit items-center gap-1.5 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground">
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to vetting
        </Link>
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <h1 className="m-0 text-2xl font-semibold text-foreground">{submission.formReference}</h1>
          <VettingStatusBadge kind="approval" status={submission.approvalState} />
          {editable && (
            <div className="ml-auto flex items-center gap-2.5">
              <Button variant="outline" size="sm" render={<Link to={paths.vettingEdit(submission.id)} />}>
                <Pencil className="h-3.5 w-3.5" /> Edit
              </Button>
              <SubmitForReview submission={submission} />
            </div>
          )}
        </div>
        <dl className="mt-4 grid grid-cols-1 gap-x-6 gap-y-3 sm:grid-cols-3">
          <DetailField label="Donor" value={<Link to={paths.donorDetail(submission.donorId)} className="hover:underline">{submission.donorCompanyName}</Link>} />
          <DetailField label="Officer" value={submission.officerFullName} />
          <DetailField label="Submitted" value={submission.submittedAt ? DATE.format(new Date(submission.submittedAt)) : 'Not yet submitted'} />
        </dl>
      </div>

      {submission.decisionReason && (
        <div role="alert" className="flex items-start gap-3 rounded-2xl border border-amber-500/40 bg-amber-500/10 p-3.75 px-4.5">
          <AlertCircle className="mt-0.5 h-4.5 w-4.5 shrink-0 text-amber-700 dark:text-amber-400" />
          <p className="m-0 text-[13px] leading-relaxed font-semibold text-foreground">
            <span className="font-extrabold">{submission.approvalState}:</span> {submission.decisionReason}
          </p>
        </div>
      )}

      {(submission.submittedAt || submission.flags.length > 0) && <FlagsPanel submission={submission} />}

      <AccordionFormLayout sectionIds={SECTION_IDS}>
        {VETTING_SECTIONS.map((section, index) => (
          <FormAccordionSection
            key={section.id}
            id={section.id}
            number={index + 1}
            title={section.title}
            icon={section.icon}
            badge={section.id === 'photos' ? <PhotosCapturedBadge photos={submission.photos} /> : undefined}
          >
            {/* display:contents keeps the rows on the section's grid while giving dt/dd a valid <dl> parent. */}
            {section.fields.length > 0 && (
              <dl className="contents">
                {section.fields.map((field) => (
                  <DetailField key={field} label={FIELD_LABELS[field]} value={formatValue(submission.details[field])} />
                ))}
              </dl>
            )}
            {fileSlots(section.id).map((slot) => (
              <AssessmentFileSlot key={slot} submissionId={submission.id} slot={slot} files={submission.files} editable={false} />
            ))}
            {section.id === 'photos' && <EvidencePhotos submissionId={submission.id} photos={submission.photos} editable={editable} />}
          </FormAccordionSection>
        ))}
      </AccordionFormLayout>
    </div>
  );
}

function FlagsPanel({ submission }: { submission: VettingSubmission }) {
  return (
    <section className="rounded-2xl border border-border bg-card p-5">
      <h2 className="m-0 mb-3 flex items-center gap-2 text-[15px] font-extrabold tracking-tight text-foreground">
        <Flag className="h-4 w-4" /> Flags
      </h2>
      {submission.flags.length === 0 ? (
        <p className="m-0 text-[13px] font-medium text-muted-foreground">No flags raised.</p>
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {submission.flags.map((flag) => (
            <li key={flag.id} className="flex items-center gap-2.5 text-[13px] font-semibold text-foreground">
              <span
                className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${
                  flag.source === 'Auto' ? 'bg-amber-500/15 text-amber-700 dark:text-amber-400' : 'bg-sky-500/15 text-sky-700 dark:text-sky-400'
                }`}
              >
                {FLAG_SOURCE_LABELS[flag.source]}
              </span>
              {flag.description}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function SubmitForReview({ submission }: { submission: VettingSubmission }) {
  const [confirming, setConfirming] = useState(false);
  const submit = useSubmitVetting(submission.id);
  const missing = submission.photos.filter((p) => !p.isCaptured).map((p) => PHOTO_CATEGORY_LABELS[p.category]);
  const missingText = missing.length > 0 ? `Missing photos: ${missing.join(', ')}` : undefined;
  // A 400 here means photos are still missing server-side; its field error carries the detail.
  const errorMessage = submit.error
    ? (submit.error.response?.data?.errors?.[0]?.message ?? submit.error.response?.data?.message ?? 'Could not submit. Please try again.')
    : null;

  const close = (open: boolean) => {
    setConfirming(open);
    if (!open) submit.reset();
  };

  return (
    <>
      <span className="flex items-center gap-2.5" title={missingText}>
        {missingText && <span className="text-xs font-semibold text-destructive">{missingText}</span>}
        <Button size="sm" disabled={missing.length > 0} onClick={() => setConfirming(true)}>
          <Send className="h-3.5 w-3.5" /> Submit for review
        </Button>
      </span>
      <Dialog open={confirming} onOpenChange={close}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Submit for review?</DialogTitle>
            <DialogDescription>
              An admin will review {submission.formReference}. It can't be edited again unless it is returned to you.
            </DialogDescription>
          </DialogHeader>
          {errorMessage && <p className="m-0 text-sm text-destructive">{errorMessage}</p>}
          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => close(false)}>
              Cancel
            </Button>
            <Button
              type="button"
              size="sm"
              disabled={submit.isPending}
              onClick={() =>
                submit.mutate(undefined, {
                  onSuccess: (updated) => {
                    close(false);
                    toast.success(`${updated.formReference} submitted for review.`);
                  },
                })
              }
            >
              {submit.isPending ? 'Submitting…' : 'Submit'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
