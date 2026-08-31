import type { ReactNode } from 'react';

interface DetailSectionCardProps {
  title: string;
  children: ReactNode;
}

export function DetailSectionCard({ title, children }: DetailSectionCardProps) {
  return (
    <div className="rounded-2xl border border-border bg-card p-6.5 shadow-[0_1px_3px_var(--shadow)]">
      <h3 className="m-0 mb-5 text-[15px] font-extrabold tracking-tight text-foreground">{title}</h3>
      <dl className="grid grid-cols-1 gap-x-6 gap-y-5 sm:grid-cols-2">{children}</dl>
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
      <dt className="mb-1.75 text-[11px] font-bold tracking-wider text-muted-foreground uppercase">{label}</dt>
      <dd className="text-sm font-semibold whitespace-pre-line text-foreground">{value ?? '—'}</dd>
    </div>
  );
}
