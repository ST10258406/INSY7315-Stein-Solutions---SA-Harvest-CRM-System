import { describe, it, expect } from 'vitest';
import {
  sendDonorEmailFormSchema,
  formValuesToSendDonorEmailRequest,
  type SendDonorEmailFormValues,
} from './sendDonorEmail.schema';

const base: SendDonorEmailFormValues = {
  to: 'donor@example.com',
  subject: 'Thanks for your support',
  body: 'Just checking in ahead of next month\'s collection.',
};

describe('sendDonorEmailFormSchema', () => {
  it('accepts a valid compose-email form', () => {
    expect(sendDonorEmailFormSchema.safeParse(base).success).toBe(true);
  });

  it('rejects an empty recipient', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, to: '' }).success).toBe(false);
  });

  it('rejects an invalid email address', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, to: 'not-an-email' }).success).toBe(false);
  });

  it.each([
    'donor@example.com,other@example.com',
    'donor@example.com;other@example.com',
    'donor@example.com, other@example.com',
  ])('rejects multiple recipients: %s', (to) => {
    const result = sendDonorEmailFormSchema.safeParse({ ...base, to });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((issue) => issue.message.includes('single recipient'))).toBe(true);
    }
  });

  it('rejects an empty subject', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, subject: '' }).success).toBe(false);
  });

  it('rejects a subject longer than 200 characters', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, subject: 'x'.repeat(201) }).success).toBe(false);
  });

  it('accepts a subject at exactly 200 characters', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, subject: 'x'.repeat(200) }).success).toBe(true);
  });

  it('rejects an empty body', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, body: '   ' }).success).toBe(false);
  });

  it('rejects a body longer than 10 000 characters', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, body: 'x'.repeat(10_001) }).success).toBe(false);
  });

  it('accepts a body at exactly 10 000 characters', () => {
    expect(sendDonorEmailFormSchema.safeParse({ ...base, body: 'x'.repeat(10_000) }).success).toBe(true);
  });
});

describe('formValuesToSendDonorEmailRequest', () => {
  it('trims whitespace from every field', () => {
    const request = formValuesToSendDonorEmailRequest({
      to: '  donor@example.com  ',
      subject: '  Subject  ',
      body: '  Body text  ',
    });
    expect(request).toEqual({ to: 'donor@example.com', subject: 'Subject', body: 'Body text' });
  });
});
