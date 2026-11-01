import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { FormField } from '@/features/donors/components/form';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import { useCreateOfficer, useUpdateOfficer } from '../hooks';
import { officerFormSchema, officerFormValuesToRequest, officerToFormValues, type OfficerFormValues } from '../schemas/officerForm.schema';
import type { VettingOfficer } from '../types';

interface OfficerFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Edit this officer; omit to create. */
  officer?: VettingOfficer;
}

export function OfficerFormDialog({ open, onOpenChange, officer }: OfficerFormDialogProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const createOfficer = useCreateOfficer();
  const updateOfficer = useUpdateOfficer();
  const defaults = officerToFormValues(officer);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<OfficerFormValues>({ resolver: zodResolver(officerFormSchema), defaultValues: defaults });

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
      const request = officerFormValuesToRequest(values);
      if (officer) await updateOfficer.mutateAsync({ id: officer.id, request });
      else await createOfficer.mutateAsync(request);
      handleOpenChange(false);
    } catch (error) {
      if (!applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError))
        setGeneralError('Could not save this officer. Please try again.');
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{officer ? 'Edit officer' : 'New officer'}</DialogTitle>
          <DialogDescription>Field officers carry out the vetting site visits. Workload is calculated automatically.</DialogDescription>
        </DialogHeader>
        <form onSubmit={onSubmit} className="flex flex-col gap-3.5">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}
          <FormField label="Full name" required registration={register('fullName')} error={errors.fullName?.message} />
          <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2">
            <FormField
              label="Initials"
              required
              maxLength={5}
              hint="Up to 5 letters, e.g. TM"
              registration={register('initials', {
                // Forced to uppercase as typed, so what is shown is what is saved.
                onChange: (event) => (event.target.value = event.target.value.toUpperCase()),
              })}
              error={errors.initials?.message}
            />
            <FormField
              label="Assigned region"
              required
              placeholder="e.g. Gauteng North"
              registration={register('assignedRegion')}
              error={errors.assignedRegion?.message}
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : officer ? 'Save changes' : 'Create officer'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
