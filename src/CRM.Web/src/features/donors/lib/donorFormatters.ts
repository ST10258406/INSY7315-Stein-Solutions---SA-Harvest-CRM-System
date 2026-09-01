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

/** Formats an ISO date-time string with both date and time, for audit-style fields (created/updated at). */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

const FILE_SIZE_UNITS = ['B', 'KB', 'MB', 'GB'];

export function formatFileSize(bytes: number): string {
  if (bytes <= 0) return '0 B';
  const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), FILE_SIZE_UNITS.length - 1);
  const value = bytes / 1024 ** exponent;
  return `${exponent === 0 ? value : value.toFixed(1)} ${FILE_SIZE_UNITS[exponent]}`;
}
