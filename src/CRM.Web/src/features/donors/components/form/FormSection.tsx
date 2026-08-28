import type { ReactNode } from 'react';

interface FormSectionProps {
  title: string;
  description?: string;
  children: ReactNode;
}

export function FormSection({ title, description, children }: FormSectionProps) {
  return (
    <div className="rounded-2xl border border-[#2B2B23] bg-[#141410] p-5">
      <h3 className="text-xs font-semibold tracking-wide text-[#B9B9AE] uppercase">{title}</h3>
      {description && <p className="mt-1 text-xs text-[#6B6B60]">{description}</p>}
      <div className="mt-4 grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">{children}</div>
    </div>
  );
}
