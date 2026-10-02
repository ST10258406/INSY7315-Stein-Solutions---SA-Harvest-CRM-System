import { ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useManagerActivity } from './hooks';
import { PERIOD_LABEL } from './lib/period';
import type { ManagerActivityPeriod } from './types';

interface ManagerActivityChartProps {
  /** Owned by DashboardPage so the dashboard export can include the period on screen. */
  period: ManagerActivityPeriod;
  onPeriodChange: (period: ManagerActivityPeriod) => void;
}

/** Donors contacted per team member, from real interaction logs (not sample data). */
export function ManagerActivityChart({ period, onPeriodChange }: ManagerActivityChartProps) {
  const { data, isLoading, isError } = useManagerActivity(period);

  const items = data?.items ?? [];
  const maxVal = Math.max(1, ...items.map((d) => d.donorsContacted));
  const total = items.reduce((acc, d) => acc + d.donorsContacted, 0);
  const average = items.length ? Math.round(total / items.length) : 0;

  return (
    <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5">
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">Summary</h2>
          <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
            Donors contacted per team member, {PERIOD_LABEL[period].toLowerCase()}.
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          aria-label={`Showing ${PERIOD_LABEL[period]}. Switch period`}
          onClick={() => onPeriodChange(period === 'weekly' ? 'monthly' : 'weekly')}
        >
          <span>{PERIOD_LABEL[period]}</span>
          <ChevronDown className="w-3.25 h-3.25 text-[var(--icon)]" />
        </Button>
      </div>

      <div className="bg-[var(--card)] rounded-xl p-4.5 shadow-[0_1px_3px_var(--shadow)]">
        {isLoading ? (
          <div className="h-[200px] animate-pulse rounded-lg bg-[var(--bar-track)]/40" role="status" aria-label="Loading summary" />
        ) : isError ? (
          <p role="alert" className="m-0 py-12 text-center text-xs font-medium text-[var(--muted-c)]">
            Couldn't load the summary. Please try again shortly.
          </p>
        ) : items.length === 0 ? (
          <p className="m-0 py-12 text-center text-xs font-medium text-[var(--muted-c)]">
            No donor contact has been logged in the {PERIOD_LABEL[period].toLowerCase()}.
          </p>
        ) : (
          <>
            <div className="flex items-stretch gap-5 mb-5">
              <div>
                <div className="text-[11.5px] font-semibold text-[var(--muted2)] mb-0.75">Total contacted</div>
                <div className="text-2xl font-extrabold tracking-tight text-[var(--ink)]">{total}</div>
              </div>
              <div className="w-px bg-[var(--divider)]" />
              <div>
                <div className="text-[11.5px] font-semibold text-[var(--muted2)] mb-0.75">Avg per manager</div>
                <div className="text-2xl font-extrabold tracking-tight text-[var(--ink)]">{average}</div>
              </div>
            </div>

            <div className="flex items-end gap-2.5 h-[170px]">
              {items.map((bar, i) => {
                const heightPx = Math.round((bar.donorsContacted / maxVal) * 118 + 8);
                return (
                  <div key={bar.userId} className="flex-1 min-w-0 flex flex-col items-center gap-1.5">
                    <span className="text-[11px] font-bold text-[var(--ink)]">{bar.donorsContacted}</span>
                    <div
                      className={`w-full rounded-t-md transition-all duration-300 ${
                        i < 3 ? 'bg-brand' : 'bg-[var(--bar-track)]'
                      }`}
                      style={{ height: `${heightPx}px` }}
                      title={`${bar.name}: ${bar.donorsContacted} donors`}
                    />
                    <span className="w-full truncate text-center text-[10.5px] font-semibold text-[var(--muted2)]">
                      {bar.name.split(' ')[0]}
                    </span>
                  </div>
                );
              })}
            </div>
          </>
        )}
      </div>
    </section>
  );
}
