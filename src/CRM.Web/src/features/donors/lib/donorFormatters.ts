/** Formats an ISO date(-time) string for display; returns an em dash for null/undefined. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });
}

/** A follow-up date is overdue once it's strictly before the start of today. */
export function isOverdue(value: string | null | undefined): boolean {
  if (!value) return false;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return false;
  const startOfToday = new Date();
  startOfToday.setHours(0, 0, 0, 0);
  return date.getTime() < startOfToday.getTime();
}

export function joinOrDash(values: string[]): string {
  return values.length > 0 ? values.join(', ') : '—';
}
