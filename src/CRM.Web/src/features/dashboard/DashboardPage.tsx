import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Filter, Download, Plus, SlidersHorizontal, ChevronDown } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { DonorKpiCards } from './DonorKpiCards';
import { OverdueFollowUpsWidget } from './OverdueFollowUpsWidget';
import { paths } from '@/routes/paths';
import { Button, buttonVariants } from '@/components/ui/button';

const DASHBOARD_CHARTS_DATA = {
  Weekly: [
    { label: 'Keegan', value: 142 },
    { label: 'Sarah', value: 89 },
    { label: 'Mike', value: 76 },
    { label: 'David', value: 45 },
    { label: 'Emma', value: 34 },
    { label: 'John', value: 26 },
  ],
  Monthly: [
    { label: 'Keegan', value: 580 },
    { label: 'Sarah', value: 420 },
    { label: 'Mike', value: 390 },
    { label: 'David', value: 210 },
    { label: 'Emma', value: 185 },
    { label: 'John', value: 120 },
  ],
};

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  const [period, setPeriod] = useState<'Weekly' | 'Monthly'>('Weekly');

  const chartItems = DASHBOARD_CHARTS_DATA[period];
  const maxVal = Math.max(...chartItems.map((d) => d.value));
  const totalContacted = chartItems.reduce((acc, d) => acc + d.value, 0);
  const avgPerManager = Math.round(totalContacted / chartItems.length);
  const top3Names = [...chartItems]
    .sort((a, b) => b.value - a.value)
    .slice(0, 3)
    .map((d) => d.label);

  return (
    <main className="flex-1 min-w-0 overflow-y-auto p-[26px_30px_34px]">
      {/* Header Banner */}
      <div className="flex items-end gap-6 flex-wrap mb-6">
        <div>
          <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">
            Welcome back{user ? `, ` : ''}
            {user && <span className="text-[var(--ink)]">{user.firstName}</span>}!
          </h1>
          <p className="m-0 mt-1.5 text-sm font-medium text-[var(--muted-c)]">
            Track donor relationships, follow-ups, and approvals.
          </p>
        </div>

        <div className="ml-auto flex items-center gap-2.5">
          <Button variant="secondary" size="sm">
            <Filter className="w-3.75 h-3.75" />
            <span>Filters</span>
          </Button>

          <Button variant="secondary" size="sm">
            <Download className="w-3.75 h-3.75" />
            <span>Export</span>
          </Button>

          <Link
            to={paths.donorNew}
            className={buttonVariants({ variant: 'default', size: 'sm' })}
          >
            <Plus className="w-4 h-4 stroke-[2.2]" />
            <span>New Donor</span>
          </Link>
        </div>
      </div>

      {/* Top Stat Cards Section */}
      <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5 mb-5">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">
              Donor activity
            </h2>
            <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
              Live counts across every donor record in the system.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="secondary" size="icon">
              <SlidersHorizontal className="w-4 h-4" />
            </Button>
          </div>
        </div>

        <DonorKpiCards />
      </section>

      {/* Main Grid: Summary Chart + Overdue Table */}
      <div className="grid grid-cols-1 lg:grid-cols-[minmax(360px,0.85fr)_1.3fr] gap-5 items-start">
        {/* Left Section: RM Activity Chart */}
        <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5">
          <div className="flex items-start justify-between gap-3 mb-4">
            <div>
              <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">
                Summary
              </h2>
              <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
                Donors contacted per relationship manager.
              </p>
            </div>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setPeriod((p) => (p === 'Weekly' ? 'Monthly' : 'Weekly'))}
            >
              <span>{period}</span>
              <ChevronDown className="w-3.25 h-3.25 text-[var(--icon)]" />
            </Button>
          </div>

          <div className="bg-[var(--card)] rounded-xl p-4.5 shadow-[0_1px_3px_var(--shadow)]">
            <div className="flex items-stretch gap-5 mb-5">
              <div>
                <div className="text-[11.5px] font-semibold text-[var(--muted2)] mb-0.75">
                  Total contacted
                </div>
                <div className="text-2xl font-extrabold tracking-tight text-[var(--ink)]">
                  {totalContacted}
                </div>
              </div>
              <div className="w-px bg-[var(--divider)]" />
              <div>
                <div className="text-[11.5px] font-semibold text-[var(--muted2)] mb-0.75">
                  Avg per manager
                </div>
                <div className="text-2xl font-extrabold tracking-tight text-[var(--ink)]">
                  {avgPerManager}
                </div>
              </div>
            </div>

            {/* Bar Graph */}
            <div className="flex items-end gap-2.5 h-[150px]">
              {chartItems.map((bar) => {
                const isTop = top3Names.includes(bar.label);
                const heightPx = Math.round((bar.value / maxVal) * 118 + 8);
                return (
                  <div key={bar.label} className="flex-1 flex flex-col items-center gap-2">
                    <div
                      className={`w-full rounded-t-md transition-all duration-300 ${
                        isTop ? 'bg-brand' : 'bg-[var(--bar-track)]'
                      }`}
                      style={{ height: `${heightPx}px` }}
                      title={`${bar.label}: ${bar.value} donors`}
                    />
                    <span className="text-[10.5px] font-semibold text-[var(--muted2)]">
                      {bar.label}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        </section>

        {/* Right Section: Overdue Follow-Ups (Issue #111) */}
        <OverdueFollowUpsWidget />
      </div>
    </main>
  );
}
