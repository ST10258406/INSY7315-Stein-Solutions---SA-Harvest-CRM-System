import { describe, it, expect } from 'vitest';
import { rejectDonorSchema } from './rejectDonor.schema';

describe('rejectDonorSchema', () => {
  it('accepts a reason of at least 10 characters', () => {
    expect(rejectDonorSchema.safeParse({ rejectionReason: 'Reg number mismatch, please resubmit.' }).success).toBe(true);
  });

  it('rejects a reason shorter than 10 characters (after trimming)', () => {
    expect(rejectDonorSchema.safeParse({ rejectionReason: '   short   ' }).success).toBe(false);
  });

  it('rejects an empty reason', () => {
    expect(rejectDonorSchema.safeParse({ rejectionReason: '' }).success).toBe(false);
  });

  it('rejects a reason over 2000 characters', () => {
    expect(rejectDonorSchema.safeParse({ rejectionReason: 'x'.repeat(2001) }).success).toBe(false);
  });
});
