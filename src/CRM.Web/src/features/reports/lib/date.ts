import { fromYmd, toYmd } from '@/lib/date';

// Moved to @/lib/date so the shared DatePicker can use them; re-exported for existing callers.
export { fromYmd, toYmd };

/** Start/end of the current calendar month, as yyyy-MM-dd — the sensible default for a report
 * that would otherwise be empty on first load. */
export function thisCalendarMonthRange(): { startDate: string; endDate: string } {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1);
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  return { startDate: toYmd(start), endDate: toYmd(end) };
}
