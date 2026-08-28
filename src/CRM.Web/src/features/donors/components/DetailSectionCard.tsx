import type { ReactNode } from 'react';

interface DetailSectionCardProps {
  title: string;
  children: ReactNode;
}

export function DetailSectionCard({ title, children }: DetailSectionCardProps) {
  return (
    <div className="rounded-2xl border border-[#2B2B23] bg-[#141410] p-5">
      <h3 className="text-xs font-semibold tracking-wide text-[#B9B9AE] uppercase">{title}</h3>
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
      <dt className="text-xs text-[#6B6B60] uppercase tracking-wide">{label}</dt>
      <dd className="mt-1 text-sm text-[#F4F4EE]">{value ?? '—'}</dd>
    </div>
  );
}
