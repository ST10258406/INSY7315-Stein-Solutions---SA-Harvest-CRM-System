import { z } from 'zod';
import type { CreateSiteVisitRequest } from '../types';

// Mirrors CreateSiteVisitCommandValidator (#156). locationLabel is optional: the API
// defaults it to the donor's legal-address suburb.
export const siteVisitFormSchema = z.object({
  donorId: z.string().trim().min(1, 'Select a donor'),
  officerId: z.string().trim().min(1, 'Select an officer'),
  scheduledDate: z.string().trim().regex(/^\d{4}-\d{2}-\d{2}$/, 'Pick a date'),
  scheduledTime: z.string().trim().regex(/^\d{2}:\d{2}$/, 'Pick a time'),
  locationLabel: z.string().trim().max(100, 'Location must be 100 characters or fewer'),
  visitNotes: z.string().trim().max(255, 'Notes must be 255 characters or fewer'),
});

export type SiteVisitFormValues = z.infer<typeof siteVisitFormSchema>;

export function emptySiteVisitFormValues(scheduledDate: string, officerId = ''): SiteVisitFormValues {
  return { donorId: '', officerId, scheduledDate, scheduledTime: '', locationLabel: '', visitNotes: '' };
}

/** Form state → POST body. The API's TimeOnly wants seconds; blank optionals are left out. */
export function siteVisitFormValuesToRequest(values: SiteVisitFormValues): CreateSiteVisitRequest {
  return {
    donorId: values.donorId,
    officerId: values.officerId,
    scheduledDate: values.scheduledDate,
    scheduledTime: `${values.scheduledTime}:00`,
    ...(values.locationLabel.trim() ? { locationLabel: values.locationLabel.trim() } : {}),
    ...(values.visitNotes.trim() ? { visitNotes: values.visitNotes.trim() } : {}),
  };
}
