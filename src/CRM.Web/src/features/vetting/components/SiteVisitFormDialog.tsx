import { useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle } from 'lucide-react';
import { DatePicker } from '@/components/common/DatePicker';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { FormField, FormSelect } from '@/features/donors/components/form';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import { DonorSelectField } from '@/features/tasks/components/DonorSelectField';
import { useCreateSiteVisit, useOfficers } from '../hooks';
import {
  emptySiteVisitFormValues,
  siteVisitFormSchema,
  siteVisitFormValuesToRequest,
  type SiteVisitFormValues,
} from '../schemas/siteVisitForm.schema';

interface SiteVisitFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Pre-fills the date (and officer) from the day view the user is looking at. */
  defaultDate: string;
  defaultOfficerId?: string;
}

export function SiteVisitFormDialog({ open, onOpenChange, defaultDate, defaultOfficerId }: SiteVisitFormDialogProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const { data: officers = [], isPending: officersLoading } = useOfficers();
  const createVisit = useCreateSiteVisit();
  const defaults = emptySiteVisitFormValues(defaultDate, defaultOfficerId);

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<SiteVisitFormValues>({ resolver: zodResolver(siteVisitFormSchema), defaultValues: defaults });
  const donorId = useWatch({ control, name: 'donorId' });

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset(defaults);
      setGeneralError(null);
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setGeneralError(null);
    try {
      await createVisit.mutateAsync(siteVisitFormValuesToRequest(values));
      handleOpenChange(false);
    } catch (error) {
      if (!applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError))
        setGeneralError('Could not schedule this visit. Please try again.');
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Schedule visit</DialogTitle>
          <DialogDescription>Book a vetting site visit at a donor for one of the field officers.</DialogDescription>
        </DialogHeader>
        <form onSubmit={onSubmit} className="flex flex-col gap-3.5">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}
          <DonorSelectField
            value={donorId}
            onChange={(id) => setValue('donorId', id, { shouldValidate: true })}
            error={errors.donorId?.message}
          />
          <FormSelect
            label="Officer"
            required
            registration={register('officerId')}
            placeholder="Select an officer"
            isLoading={officersLoading}
            options={officers.map((o) => ({ value: o.id, label: `${o.fullName} (${o.initials})` }))}
            error={errors.officerId?.message}
          />
          <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label htmlFor="visit-date" className="text-xs font-bold text-foreground">
                Date <span aria-hidden className="text-destructive">*</span>
              </label>
              <Controller
                control={control}
                name="scheduledDate"
                render={({ field }) => (
                  <DatePicker id="visit-date" variant="field" value={field.value} onChange={field.onChange} invalid={!!errors.scheduledDate} />
                )}
              />
              {errors.scheduledDate && <span className="text-xs text-destructive">{errors.scheduledDate.message}</span>}
            </div>
            <FormField label="Time" required type="time" registration={register('scheduledTime')} error={errors.scheduledTime?.message} />
          </div>
          <FormField
            label="Location"
            placeholder="Defaults to the donor's suburb"
            hint="Leave blank to use the suburb on the donor's legal address."
            registration={register('locationLabel')}
            error={errors.locationLabel?.message}
          />
          <FormField label="Notes" registration={register('visitNotes')} error={errors.visitNotes?.message} />
          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Scheduling…' : 'Schedule visit'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
