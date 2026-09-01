/** Up to two initials from a person/company name, for avatar chips. */
export function getInitials(name: string | null | undefined): string {
  if (!name) return '—';
  const parts = name.split(/\s+/).filter((word) => /[A-Za-z0-9]/.test(word[0]));
  if (parts.length === 0) return '—';
  return parts
    .slice(0, 2)
    .map((word) => word[0].toUpperCase())
    .join('');
}

// A fixed palette (not per-person data) that avatar chips cycle through by
// name hash, so the same name always lands on the same color.
const AVATAR_PALETTE = ['#3F5D46', '#2E5A78', '#7A3B4E', '#5B4B8A', '#8A5A2B', '#4A5B2E', '#3B4A40'];

/** Deterministic avatar background color derived from a name's hash. */
export function getAvatarColor(name: string | null | undefined): string {
  if (!name) return '#5F5F57';
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) | 0;
  }
  return AVATAR_PALETTE[Math.abs(hash) % AVATAR_PALETTE.length];
}
