import { describe, it, expect } from 'vitest';
import { getMonthlyTrend } from './trend';

describe('getMonthlyTrend', () => {
  it('returns "up" with a percentage label when thisMonth > lastMonth', () => {
    expect(getMonthlyTrend(35, 30)).toEqual({ direction: 'up', label: '17% vs last month' });
  });

  it('returns "down" with a percentage label when thisMonth < lastMonth', () => {
    expect(getMonthlyTrend(19, 20)).toEqual({ direction: 'down', label: '5% vs last month' });
  });

  it('returns "flat" (not up or down) when the values are equal, even when nonzero', () => {
    expect(getMonthlyTrend(12, 12)).toEqual({ direction: 'flat', label: 'No change vs last month' });
  });

  it('returns "flat" when both values are zero', () => {
    expect(getMonthlyTrend(0, 0)).toEqual({ direction: 'flat', label: 'No change vs last month' });
  });

  it('falls back to an absolute difference when lastMonth is 0, instead of an infinite percentage', () => {
    expect(getMonthlyTrend(5, 0)).toEqual({ direction: 'up', label: '5 donors vs last month' });
  });

  it('uses singular "donor" for an absolute difference of exactly 1', () => {
    expect(getMonthlyTrend(1, 0)).toEqual({ direction: 'up', label: '1 donor vs last month' });
  });

  it('rounds the percentage to the nearest whole number', () => {
    expect(getMonthlyTrend(10, 3)).toEqual({ direction: 'up', label: '233% vs last month' });
  });
});
