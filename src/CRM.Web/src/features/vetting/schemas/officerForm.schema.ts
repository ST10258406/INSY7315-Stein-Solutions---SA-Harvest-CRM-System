import { z } from 'zod';
import type { CreateOfficerRequest, VettingOfficer } from '../types';

// Mirrors OfficerRules (CRM.Application, #156): full name ≤100, initials 1–5 uppercase letters, region ≤50.
export const officerFormSchema = z.object({
  fullName: z.string().trim().min(1, 'Enter the officer’s full name').max(100, 'Full name must be 100 characters or fewer'),
  initials: z
    .string()
    .trim()
    .min(1, 'Enter initials')
    .max(5, 'Initials can be at most 5 letters')
    .regex(/^[A-Z]+$/, 'Initials must be uppercase letters'),
  assignedRegion: z.string().trim().min(1, 'Enter the assigned region').max(50, 'Region must be 50 characters or fewer'),
});

export type OfficerFormValues = z.infer<typeof officerFormSchema>;

export function officerToFormValues(officer?: VettingOfficer): OfficerFormValues {
  return {
    fullName: officer?.fullName ?? '',
    initials: officer?.initials ?? '',
    assignedRegion: officer?.assignedRegion ?? '',
  };
}

export function officerFormValuesToRequest(values: OfficerFormValues): CreateOfficerRequest {
  return { fullName: values.fullName.trim(), initials: values.initials.trim(), assignedRegion: values.assignedRegion.trim() };
}
