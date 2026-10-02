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
