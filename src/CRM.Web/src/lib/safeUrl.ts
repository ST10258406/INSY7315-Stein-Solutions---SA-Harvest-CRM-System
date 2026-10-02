/**
 * Returns the URL only if it parses as an absolute http(s) URL, otherwise null. Use it before
 * putting any server-supplied string into an href, so `javascript:` / `data:` values never
 * become clickable links.
 */
export function toSafeHttpUrl(value: string | null | undefined): string | null {
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
  } catch {
    return null;
  }
}
