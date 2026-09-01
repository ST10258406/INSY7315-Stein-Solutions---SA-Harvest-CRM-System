import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Filter, Download, Plus, SlidersHorizontal, ChevronDown } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { DonorKpiCards } from './DonorKpiCards';
import { useDonors } from '@/features/donors/hooks';
import { paths } from '@/routes/paths';
import { Button, buttonVariants } from '@/components/ui/button';

// Simple helper for initials like in Demo UI
function getInitials(name: string) {
  if (!name) return '??';
  const parts = name.split(' ');
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  return name.substring(0, 2).toUpperCase();
}

// Simple palette like in Demo UI
const MANAGER_PALETTE = [
  '#3F5D46', '#4B5267', '#6A4C3D', '#544766', '#59604C', '#684551', '#425A63'
];

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

const getStatusClasses = (status: string) => {
  switch (status) {
    case 'Pending Review':
    case 'PendingReview':
      return { text: 'text-amber-700 dark:text-amber-400', dot: 'bg-amber-500' };
    case 'Active':
      return { text: 'text-emerald-700 dark:text-emerald-400', dot: 'bg-emerald-500' };
    case 'Lapsed':
      return { text: 'text-rose-700 dark:text-rose-400', dot: 'bg-rose-500' };
    default:
      return { text: 'text-muted-foreground', dot: 'bg-muted-foreground' };
  }
};

const formatStatusText = (status: string) => {
  if (status === 'PendingReview') return 'Pending Review';
  return status;
};

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user);
  const [period, setPeriod] = useState<'Weekly' | 'Monthly'>('Weekly');
  
  // Use a standard query for the table as a placeholder since we don't have a specific "overdue" endpoint yet,
  // but we can pass followUpBefore if the API supports it, or just use default.
  // The Demo UI used a static OVERDUE_FOLLOWUPS list. We'll use the real query data.
  const { data: overdueData, isPending } = useDonors({ pageSize: 5 });

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

        {/* Right Section: Overdue Follow-Ups Table */}
        <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-5 min-w-0">
          <div className="flex items-start justify-between mb-4">
            <div>
              <h2 className="m-0 mb-1 text-base font-bold tracking-tight text-[var(--ink)]">
                Overdue Follow-Ups
              </h2>
              <p className="m-0 text-xs font-medium text-[var(--muted-c)]">
                Recent donors that may require attention.
              </p>
            </div>
            <Button variant="secondary" size="icon">
              <SlidersHorizontal className="w-4 h-4" />
            </Button>
          </div>

          <div className="bg-[var(--card)] rounded-xl p-[6px_18px_10px] shadow-[0_1px_3px_var(--shadow)] overflow-x-auto">
            <table className="w-full border-collapse min-w-[560px]">
              <thead>
                <tr>
                  <th className="text-left py-3.5 pr-2 pl-0 text-[11.5px] font-semibold text-[var(--muted2)]">
                    Donor
                  </th>
                  <th className="text-left py-3.5 pr-2 pl-0 text-[11.5px] font-semibold text-[var(--muted2)]">
                    Reference
                  </th>
                  <th className="text-left py-3.5 pr-2 pl-0 text-[11.5px] font-semibold text-[var(--muted2)]">
                    Status
                  </th>
                  <th className="text-left py-3.5 pr-2 pl-0 text-[11.5px] font-semibold text-[var(--muted2)]">
                    Action
                  </th>
                </tr>
              </thead>
              <tbody>
                {isPending ? (
                  <tr>
                    <td colSpan={4} className="py-4 text-sm text-[var(--muted-c)] text-center">Loading...</td>
                  </tr>
                ) : overdueData?.data && overdueData.data.length > 0 ? (
                  overdueData.data.map((row, idx) => {
                    const s = getStatusClasses(row.status);
                    const name = row.companyName || 'Unknown';
                    const initials = getInitials(name);
                    const avatarBg = MANAGER_PALETTE[idx % MANAGER_PALETTE.length];
                    
                    return (
                      <tr key={row.id} className="border-t border-[var(--hair)] hover:bg-[var(--row-hover)] transition-colors">
                        <td className="py-3.25 pr-2 pl-0">
                          <div className="flex items-center gap-2.5">
                            <div
                              className="shrink-0 w-8 h-8 rounded-full text-white text-[11px] font-bold flex items-center justify-center"
                              style={{ backgroundColor: avatarBg }}
                            >
                              {initials}
                            </div>
                            <span className="text-[13.5px] font-semibold text-[var(--ink)] whitespace-nowrap">
                              {name}
                            </span>
                          </div>
                        </td>
                        <td className="py-3.25 pr-2 pl-0 text-13 text-[var(--muted-c)] font-medium whitespace-nowrap">
                          {row.id.substring(0, 8)}
                        </td>
                        <td className="py-3.25 pr-2 pl-0">
                          <span
                            className={`inline-flex items-center gap-1.75 text-[12.5px] font-bold whitespace-nowrap ${s.text}`}
                          >
                            <span
                              className={`w-1.5 h-1.5 rounded-full ${s.dot}`}
                            />
                            <span>{formatStatusText(row.status)}</span>
                          </span>
                        </td>
                        <td className="py-3.25 pr-0 pl-2 text-left whitespace-nowrap">
                          <Link
                            to={paths.donorDetail(row.id)}
                            className="text-13 font-bold text-[var(--ink)] border-b-[1.5px] border-brand pb-0.25 hover:text-brand cursor-pointer transition-colors"
                          >
                            View donor
                          </Link>
                        </td>
                      </tr>
                    );
                  })
                ) : (
                  <tr>
                    <td colSpan={4} className="py-4 text-sm text-[var(--muted-c)] text-center">No recent donors found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </main>
  );
}
