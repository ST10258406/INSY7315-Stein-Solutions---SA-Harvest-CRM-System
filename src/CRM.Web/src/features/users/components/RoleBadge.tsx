// Mirrors the design's ROLES palette (SA Harvest CRM Users.dc.html) — SuperAdmin
// gets the brand-yellow ring since it's the highest-privilege role.
const ROLE_STYLES: Record<string, string> = {
  Marketing: 'bg-sky-500/15 text-sky-700 dark:text-sky-300',
  Procurement: 'bg-teal-500/15 text-teal-700 dark:text-teal-300',
  Admin: 'bg-violet-500/15 text-violet-700 dark:text-violet-300',
  SuperAdmin: 'bg-[var(--ink)] text-[var(--brand)] ring-1 ring-[var(--brand)]',
};

export function RoleBadge({ role }: { role: string }) {
  const style = ROLE_STYLES[role] ?? 'bg-muted text-muted-foreground';

  return (
    <span className={`inline-flex items-center whitespace-nowrap rounded-2xl px-3 py-1 text-[11.5px] font-bold ${style}`}>
      {role}
    </span>
  );
}
