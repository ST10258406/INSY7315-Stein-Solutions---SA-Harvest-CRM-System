import { describe, it, expect } from 'vitest';
import {
  sendPublicFormInviteFormSchema,
  defaultSendPublicFormInviteValues,
  formValuesToSendPublicFormInviteRequest,
  type SendPublicFormInviteFormValues,
} from './sendPublicFormInvite.schema';

const base: SendPublicFormInviteFormValues = {
  ...defaultSendPublicFormInviteValues,
  to: 'prospect@example.com',
};

describe('sendPublicFormInviteFormSchema', () => {
  it('accepts a valid invite form', () => {
    expect(sendPublicFormInviteFormSchema.safeParse(base).success).toBe(true);
  });

  it('rejects an empty recipient', () => {
    expect(sendPublicFormInviteFormSchema.safeParse({ ...base, to: '' }).success).toBe(false);
  });

  it('rejects an invalid email address', () => {
    expect(sendPublicFormInviteFormSchema.safeParse({ ...base, to: 'not-an-email' }).success).toBe(false);
  });

  it.each(['a@example.com,b@example.com', 'a@example.com;b@example.com'])(
    'rejects multiple recipients: %s',
    (to) => {
      const result = sendPublicFormInviteFormSchema.safeParse({ ...base, to });
      expect(result.success).toBe(false);
      if (!result.success) {
        expect(result.error.issues.some((issue) => issue.message.includes('single recipient'))).toBe(true);
      }
    },
  );

  it('rejects an empty subject', () => {
    expect(sendPublicFormInviteFormSchema.safeParse({ ...base, subject: '' }).success).toBe(false);
  });

  it('rejects a subject longer than 200 characters', () => {
    expect(sendPublicFormInviteFormSchema.safeParse({ ...base, subject: 'x'.repeat(201) }).success).toBe(false);
  });

  it('rejects an empty body', () => {
    expect(sendPublicFormInviteFormSchema.safeParse({ ...base, body: '   ' }).success).toBe(false);
  });

  it('rejects a body longer than 10 000 characters', () => {
    expect(sendPublicFormInviteFormSchema.safeParse({ ...base, body: 'x'.repeat(10_001) }).success).toBe(false);
  });
});

describe('formValuesToSendPublicFormInviteRequest', () => {
  it('trims whitespace from every field', () => {
    const request = formValuesToSendPublicFormInviteRequest({
      to: '  prospect@example.com  ',
      subject: '  Subject  ',
      body: '  Body text  ',
    });
    expect(request).toEqual({ to: 'prospect@example.com', subject: 'Subject', body: 'Body text' });
  });
});
