import { z } from 'zod';

// Backend RejectDonorCommandValidator only requires NotEmpty (max 2000). The
// Claude design asks for a minimum of 10 characters so the relationship manager
// gets an actionable reason — the stricter client rule is intentional.
const MIN_REASON = 10;
const MAX_REASON = 2_000;

export const rejectDonorSchema = z.object({
  rejectionReason: z
    .string()
    .trim()
    .min(MIN_REASON, `Give at least ${MIN_REASON} characters so the relationship manager knows what to fix`)
    .max(MAX_REASON, `Keep it under ${MAX_REASON.toLocaleString()} characters`),
});

export type RejectDonorFormValues = z.infer<typeof rejectDonorSchema>;

export const REJECT_REASON_MIN = MIN_REASON;
