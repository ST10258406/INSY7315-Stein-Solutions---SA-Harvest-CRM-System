import { z } from 'zod';
import type { SendPublicFormInviteRequest } from '../types';

// Mirrors SendPublicFormInviteCommandValidator (CRM.Application) exactly:
//  - to:      required, single recipient only (no comma/semicolon lists), valid email
//  - subject: required, max 200
//  - body:    required, max 10 000
const MAX_SUBJECT = 200;
const MAX_BODY = 10_000;

function isSingleRecipient(value: string): boolean {
  return !value.includes(',') && !value.includes(';');
}

export const sendPublicFormInviteFormSchema = z.object({
  to: z
    .string()
    .trim()
    .min(1, 'Enter a recipient email address')
    .refine(isSingleRecipient, 'Only a single recipient is allowed — remove any comma- or semicolon-separated addresses')
    .refine((value) => z.string().email().safeParse(value).success, 'Enter a valid email address'),
  subject: z
    .string()
    .trim()
    .min(1, 'Enter a subject')
    .max(MAX_SUBJECT, `Subject must be ${MAX_SUBJECT} characters or fewer`),
  body: z
    .string()
    .trim()
    .min(1, 'Enter a message')
    .max(MAX_BODY, `Keep it under ${MAX_BODY.toLocaleString()} characters`),
});

export type SendPublicFormInviteFormValues = z.infer<typeof sendPublicFormInviteFormSchema>;

export const defaultSendPublicFormInviteValues: SendPublicFormInviteFormValues = {
  to: '',
  subject: "You're invited to become an SA Harvest donor",
  body: "Hi,\n\nWe'd love to have you on board as an SA Harvest donor. Please complete our short registration form to get started — it only takes a few minutes.\n\nThanks,\nSA Harvest",
};

export function formValuesToSendPublicFormInviteRequest(
  values: SendPublicFormInviteFormValues,
): SendPublicFormInviteRequest {
  return {
    to: values.to.trim(),
    subject: values.subject.trim(),
    body: values.body.trim(),
  };
}
