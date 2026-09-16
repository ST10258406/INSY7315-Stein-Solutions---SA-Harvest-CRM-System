export type TrendDirection = 'up' | 'down' | 'flat';

export interface StatsCardTrend {
  direction: TrendDirection;
  label: string;
}

/**
 * Trend for the donorsContactedThisMonth StatsCard vs donorsContactedLastMonth
 * (Issue #72). Percentage-based normally; falls back to an absolute
 * difference when lastMonth is 0, where a percent change is undefined/infinite.
 */
export function getMonthlyTrend(thisMonth: number, lastMonth: number): StatsCardTrend {
  if (thisMonth === lastMonth) {
    return { direction: 'flat', label: 'No change vs last month' };
  }

  const direction: TrendDirection = thisMonth > lastMonth ? 'up' : 'down';

  if (lastMonth === 0) {
    const diff = Math.abs(thisMonth - lastMonth);
    return { direction, label: `${diff} ${diff === 1 ? 'donor' : 'donors'} vs last month` };
  }

  const percentChange = ((thisMonth - lastMonth) / lastMonth) * 100;
  const rounded = Math.round(Math.abs(percentChange));
  return { direction, label: `${rounded}% vs last month` };
}
