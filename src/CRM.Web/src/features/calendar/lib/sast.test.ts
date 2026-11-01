import { describe, it, expect } from 'vitest';
import { addDaysYmd, monthBounds, sastYmd } from './sast';

describe('sast helpers', () => {
  it('puts 23:30 SAST on the same day and 00:30 SAST on the next', () => {
    expect(sastYmd('2026-10-14T21:30:00Z')).toBe('2026-10-14'); // 23:30 SAST
    expect(sastYmd('2026-10-14T22:30:00Z')).toBe('2026-10-15'); // 00:30 SAST
  });

  it('adds days across month and year ends', () => {
    expect(addDaysYmd('2026-10-01', -1)).toBe('2026-09-30');
    expect(addDaysYmd('2026-12-31', 1)).toBe('2027-01-01');
  });

  it('returns month bounds, including February in a leap year', () => {
    expect(monthBounds('2026-10-20')).toEqual({ first: '2026-10-01', last: '2026-10-31' });
    expect(monthBounds('2028-02-10')).toEqual({ first: '2028-02-01', last: '2028-02-29' });
  });
});
