/**
 * SAST day helpers for the calendar (Issue 104). South Africa is UTC+2 all year (no daylight
 * saving), so a fixed offset is exact. Dates come from the API as UTC instants; grouping them by
 * UTC or by the browser's own timezone would put a task due at 00:30 SAST on the previous day.
 */
const SAST_OFFSET_MS = 2 * 60 * 60 * 1000;

/** yyyy-MM-dd of the SAST calendar day an ISO instant falls on. */
export function sastYmd(iso: string | Date): string {
  const ms = (typeof iso === 'string' ? new Date(iso) : iso).getTime();
  return new Date(ms + SAST_OFFSET_MS).toISOString().slice(0, 10);
}

/** yyyy-MM-dd shifted by whole days (calendar arithmetic, no timezone involved). */
export function addDaysYmd(ymd: string, days: number): string {
  const date = new Date(`${ymd}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

/** First and last yyyy-MM-dd of the month containing `ymd`. */
export function monthBounds(ymd: string): { first: string; last: string } {
  const [year, month] = ymd.split('-').map(Number);
  const first = `${ymd.slice(0, 7)}-01`;
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return { first, last: `${ymd.slice(0, 7)}-${String(lastDay).padStart(2, '0')}` };
}
