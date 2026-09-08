/** Human label for every InteractionType, keyed by the raw backend string. */
const LABELS: Record<string, string> = {
  Note: 'Note',
  Call: 'Call',
  Email: 'Email',
  Meeting: 'Meeting',
  FormSubmission: 'Form Submission',
  FollowUp: 'Follow-up',
};

export function interactionTypeLabel(type: string): string {
  return LABELS[type] ?? type;
}

const RELATIVE_UNITS: Array<{ limit: number; div: number; unit: Intl.RelativeTimeFormatUnit }> = [
  { limit: 60, div: 1, unit: 'second' },
  { limit: 3600, div: 60, unit: 'minute' },
  { limit: 86400, div: 3600, unit: 'hour' },
  { limit: 604800, div: 86400, unit: 'day' },
  { limit: 2629800, div: 604800, unit: 'week' },
  { limit: 31557600, div: 2629800, unit: 'month' },
  { limit: Infinity, div: 31557600, unit: 'year' },
];

const rtf = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });

/** "just now", "2 days ago", "1 month ago" — matches the Claude design's timeline. */
export function formatRelativeTime(value: string): string {
  const then = new Date(value).getTime();
  if (Number.isNaN(then)) return '';
  const diffSeconds = (then - Date.now()) / 1000;
  if (Math.abs(diffSeconds) < 45) return 'just now';
  for (const { limit, div, unit } of RELATIVE_UNITS) {
    if (Math.abs(diffSeconds) < limit) {
      return rtf.format(Math.round(diffSeconds / div), unit);
    }
  }
  return '';
}
