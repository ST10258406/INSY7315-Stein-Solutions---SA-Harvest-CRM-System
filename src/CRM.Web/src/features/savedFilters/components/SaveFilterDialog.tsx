import { useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import { useCreateSavedFilter } from '../hooks';
import { filterQueryString, saveFilterSchema, type SaveFilterFormValues } from '../schemas/saveFilterSchema';
import type { ApiError } from '../types';

interface SaveFilterDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** Saves the donor list's current filters (from the URL) under a name. Issue 103. */
export function SaveFilterDialog({ open, onOpenChange }: SaveFilterDialogProps) {
  const { search } = useLocation();
  const createFilter = useCreateSavedFilter();
  const [generalError, setGeneralError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<SaveFilterFormValues>({ resolver: zodResolver(saveFilterSchema), defaultValues: { name: '' } });

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset({ name: '' });
      setGeneralError(null);
      createFilter.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async ({ name }) => {
    setGeneralError(null);
    try {
      await createFilter.mutateAsync({ name, page: 'Donors', queryString: filterQueryString(search) });
      handleOpenChange(false);
    } catch (error) {
      const apiError = error as ApiError;
      if (apiError.response?.status === 409) {
        setError('name', { message: 'You already have a saved filter with this name.' });
      } else if (!applyServerErrors(apiError, setError)) {
        setGeneralError('Could not save this filter. Please try again.');
      }
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Save filters</DialogTitle>
          <DialogDescription>Save the current search, filters and sort to reuse them from the rail.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-3">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          <div className="flex flex-col gap-2">
            <label htmlFor="saved-filter-name" className="text-xs font-bold text-foreground">
              Name
            </label>
            <input
              id="saved-filter-name"
              autoFocus
              {...register('name')}
              placeholder="e.g. Gauteng donors with overdue follow-ups"
              aria-invalid={!!errors.name}
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.name ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.name && <span className="text-xs text-destructive">{errors.name.message}</span>}
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
