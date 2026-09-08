import { describe, it, expect } from 'vitest';
import {
  logInteractionFormSchema,
  formValuesToLogInteractionRequest,
  type LogInteractionFormValues,
} from './logInteraction.schema';

// Local calendar date (matches what <input type="date"> yields and what the
// schema compares against) — not UTC, so it stays stable near midnight.
const yyyyMmDd = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
const daysFromNow = (days: number) => {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d;
};
const tomorrow = () => daysFromNow(1);
const yesterday = () => daysFromNow(-1);

const base: LogInteractionFormValues = {
  interactionType: 'Call',
  subject: 'Surplus forecast',
  body: 'Confirmed weekly chilled surplus volume.',
  followUpDate: '',
};

describe('logInteractionFormSchema', () => {
  it('accepts a minimal valid interaction (type + body only)', () => {
    const result = logInteractionFormSchema.safeParse({ interactionType: 'Note', body: 'A note', subject: '', followUpDate: '' });
    expect(result.success).toBe(true);
  });

  it('rejects an empty body', () => {
    const result = logInteractionFormSchema.safeParse({ ...base, body: '   ' });
    expect(result.success).toBe(false);
  });

  it('rejects an interaction type that is not staff-loggable', () => {
    const result = logInteractionFormSchema.safeParse({ ...base, interactionType: 'FollowUp' });
    expect(result.success).toBe(false);
  });

  it('rejects a subject longer than 255 characters', () => {
    const result = logInteractionFormSchema.safeParse({ ...base, subject: 'x'.repeat(256) });
    expect(result.success).toBe(false);
  });

  it('rejects a follow-up date of today or earlier', () => {
    expect(logInteractionFormSchema.safeParse({ ...base, followUpDate: yyyyMmDd(new Date()) }).success).toBe(false);
    expect(logInteractionFormSchema.safeParse({ ...base, followUpDate: yyyyMmDd(yesterday()) }).success).toBe(false);
  });

  it('accepts a follow-up date in the future', () => {
    expect(logInteractionFormSchema.safeParse({ ...base, followUpDate: yyyyMmDd(tomorrow()) }).success).toBe(true);
  });
});

describe('formValuesToLogInteractionRequest', () => {
  it('drops blank subject and follow-up date', () => {
    const request = formValuesToLogInteractionRequest({ interactionType: 'Note', subject: '  ', body: '  hello  ', followUpDate: '' });
    expect(request).toEqual({ interactionType: 'Note', body: 'hello' });
  });

  it('promotes the follow-up date to an ISO UTC instant', () => {
    const date = yyyyMmDd(tomorrow());
    const request = formValuesToLogInteractionRequest({ ...base, followUpDate: date });
    expect(request.followUpDate).toBe(new Date(`${date}T00:00:00`).toISOString());
    expect(request.subject).toBe('Surplus forecast');
  });
});
