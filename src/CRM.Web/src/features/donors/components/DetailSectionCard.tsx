import type { ReactNode } from 'react';

interface DetailSectionCardProps {
  title: string;
  children: ReactNode;
}

export function DetailSectionCard({ title, children }: DetailSectionCardProps) {
  return (
    <div className="rounded-2xl border border-border bg-card p-5">
      <h3 className="text-xs font-semibold tracking-wide text-muted-foreground uppercase">{title}</h3>
      <dl className="mt-4 grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">{children}</dl>
    </div>
  );
}

interface DetailFieldProps {
  label: string;
  value: ReactNode;
  fullWidth?: boolean;
}

export function DetailField({ label, value, fullWidth }: DetailFieldProps) {
  return (
    <div className={fullWidth ? 'sm:col-span-2' : undefined}>
      <dt className="text-xs text-muted-foreground uppercase tracking-wide">{label}</dt>
      <dd className="mt-1 text-sm text-foreground">{value ?? '—'}</dd>
    </div>
  );
}
