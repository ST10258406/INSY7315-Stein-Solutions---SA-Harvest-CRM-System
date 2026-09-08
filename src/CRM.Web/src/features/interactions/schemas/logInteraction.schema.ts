import { z } from 'zod';
import { LOGGABLE_INTERACTION_TYPES, type LogInteractionRequest } from '../types';

// Mirrors LogInteractionCommandValidator (CRM.Application) exactly:
//  - interactionType: required, must be a known InteractionType
//  - subject:        optional, max 255
//  - body:           required, non-empty, max 10 000
//  - followUpDate:   optional; when set, must be strictly in the future
const MAX_SUBJECT = 255;
const MAX_BODY = 10_000;

function isFutureCalendarDate(value: string | undefined): boolean {
  if (!value) return true; // optional — emptiness handled by the field being optional
  const picked = new Date(`${value}T00:00:00`);
  if (Number.isNaN(picked.getTime())) return false;
  const startOfToday = new Date();
  startOfToday.setHours(0, 0, 0, 0);
  // Backend rejects "today" too (followUpDate > DateTime.UtcNow, and a date-only
  // value lands at midnight), so require strictly after today.
  return picked.getTime() > startOfToday.getTime();
}

export const logInteractionFormSchema = z.object({
  interactionType: z.enum(LOGGABLE_INTERACTION_TYPES, { message: 'Select an interaction type' }),
  subject: z.string().trim().max(MAX_SUBJECT, `Subject must be ${MAX_SUBJECT} characters or fewer`).optional(),
  body: z
    .string()
    .trim()
    .min(1, 'Describe what happened')
    .max(MAX_BODY, `Keep it under ${MAX_BODY.toLocaleString()} characters`),
  followUpDate: z
    .string()
    .trim()
    .optional()
    .refine(isFutureCalendarDate, { message: 'Follow-up date must be in the future' }),
});

export type LogInteractionFormValues = z.infer<typeof logInteractionFormSchema>;

export const emptyLogInteractionValues: LogInteractionFormValues = {
  interactionType: 'Note',
  subject: '',
  body: '',
  followUpDate: '',
};

/** Form state → POST body: drop blanks, promote the date to an ISO UTC instant. */
export function formValuesToLogInteractionRequest(values: LogInteractionFormValues): LogInteractionRequest {
  const subject = values.subject?.trim();
  const followUpDate = values.followUpDate?.trim();

  return {
    interactionType: values.interactionType,
    body: values.body.trim(),
    ...(subject ? { subject } : {}),
    ...(followUpDate ? { followUpDate: new Date(`${followUpDate}T00:00:00`).toISOString() } : {}),
  };
}
