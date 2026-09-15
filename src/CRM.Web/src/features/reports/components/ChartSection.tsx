import type { ReactNode } from 'react';
import { TriangleAlert } from 'lucide-react';

export const CHART_HEIGHT = 280;

interface ChartSectionProps {
  title: string;
  description: string;
  /** Optional controls rendered next to the header (e.g. a sort toggle). */
  actions?: ReactNode;
  isLoading: boolean;
  isError: boolean;
  isEmpty: boolean;
  emptyTitle: string;
  emptyMessage: string;
  onRetry?: () => void;
  children: ReactNode;
}

/**
 * Shared shell for every report chart, styled to match the existing
 * dashboard widgets (OverdueFollowUpsWidget, DonorKpiCards): a `--soft`
 * outer section with a heading, wrapping a `--card` inner surface that
 * holds the chart itself.
 *
 * Resolves state in a fixed order — loading, then error, then empty, then
 * the chart — so a genuinely empty report is never confused with one still
 * loading or one that failed.
 */
export function ChartSection({
  title,
  description,
  actions,
  isLoading,
  isError,
  isEmpty,
  emptyTitle,
  emptyMessage,
  onRetry,
  children,
}: ChartSectionProps) {
  return (
    <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5">
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">{title}</h2>
          <p className="m-0 text-xs font-medium text-[var(--muted-c)]">{description}</p>
        </div>
        {actions && !isLoading && !isError && !isEmpty && <div className="shrink-0">{actions}</div>}
      </div>

      <div className="bg-[var(--card)] rounded-xl p-4.5 shadow-[0_1px_3px_var(--shadow)]">
        {isLoading ? (
          <ChartSkeleton />
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 py-10 text-center" style={{ minHeight: CHART_HEIGHT }}>
            <TriangleAlert className="h-5 w-5 text-muted-foreground" />
            <p className="m-0 text-[13px] font-semibold text-foreground">This report couldn't be loaded</p>
            {onRetry && (
              <button
                type="button"
                onClick={onRetry}
                className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
              >
                Retry
              </button>
            )}
          </div>
        ) : isEmpty ? (
          <div
            className="flex flex-col items-center gap-2.5 py-10 text-center"
            style={{ minHeight: CHART_HEIGHT }}
          >
            <p className="m-0 text-[13px] font-bold tracking-tight text-foreground">{emptyTitle}</p>
            <p className="m-0 max-w-xs text-[11.5px] font-medium text-muted-foreground">{emptyMessage}</p>
          </div>
        ) : (
          children
        )}
      </div>
    </section>
  );
}

// Matches OverdueFollowUpsWidget's loading rows: flat `animate-pulse` bars
// against `--skel`, the established pattern rather than a new skeleton.
function ChartSkeleton() {
  return (
    <div
      aria-busy="true"
      aria-label={`Loading chart`}
      style={{ height: CHART_HEIGHT }}
      className="flex items-end gap-3"
    >
      {[55, 85, 40, 70, 30, 60, 45].map((h, i) => (
        <div key={i} className="flex-1 animate-pulse rounded-t-md bg-[var(--skel)]" style={{ height: `${h}%` }} />
      ))}
    </div>
  );
}
