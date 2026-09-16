/** yyyy-MM-dd in local time — never `toISOString()`, which shifts across UTC at day boundaries. */
export function toYmd(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

/** Inverse of {@link toYmd}. Constructs in local time — `new Date(str)` parses as UTC and can
 * land on the wrong local day. */
export function fromYmd(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** Start/end of the current calendar month, as yyyy-MM-dd — the sensible default for a report
 * that would otherwise be empty on first load. */
export function thisCalendarMonthRange(): { startDate: string; endDate: string } {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1);
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  return { startDate: toYmd(start), endDate: toYmd(end) };
}
