import type { ReactNode } from 'react';

export function PublicCard({ children }: { children: ReactNode }) {
  return (
    <div className="rounded-2xl border border-[var(--border)] bg-[#F6F6F3] p-6 pt-7.5 pb-6.5 shadow-[0_1px_3px_var(--shadow)] sm:p-8 sm:pt-7.5">
      {children}
    </div>
  );
}
