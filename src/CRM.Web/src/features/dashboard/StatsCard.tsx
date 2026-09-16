import type { LucideIcon } from 'lucide-react';

interface StatsCardProps {
  icon: LucideIcon;
  label: string;
  value: number | undefined;
  isLoading: boolean;
  isError: boolean;
}

/** One tile of `StatsCards`. Matches the card styling DonorKpiCards established in Sprint 3. */
export function StatsCard({ icon: Icon, label, value, isLoading, isError }: StatsCardProps) {
  return (
    <div className="min-w-[220px] flex-1 rounded-xl bg-[var(--card)] p-[18px] shadow-[0_1px_3px_var(--shadow)]">
      <div className="mb-4 flex items-center gap-2.25">
        <div className="flex h-7.5 w-7.5 items-center justify-center rounded-lg bg-[var(--icon-bg)] text-[var(--ink)]">
          <Icon className="h-4 w-4" />
        </div>
        <span className="text-sm font-bold text-[var(--ink)]">{label}</span>
      </div>
      <div className="text-[28px] font-extrabold tracking-tight text-[var(--ink)]">
        {isLoading ? (
          <div className="h-8.5 w-16 animate-pulse rounded-md bg-[var(--skel)]" aria-hidden />
        ) : isError ? (
          <span className="text-[var(--muted-c)]" title="Couldn't load this count">
            —
          </span>
        ) : (
          (value ?? 0)
        )}
      </div>
    </div>
  );
}
