import { useMemo, useState } from 'react';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Button } from '@/components/ui/button';
import { useDonorsContactedReport, defaultDonorsContactedFilters } from '../hooks/useDonorsContactedReport';
import type { DonorsContactedFilters, ManagerContacted } from '../types';
import { CHART_HEIGHT, ChartSection } from './ChartSection';
import { ExportReportButton } from './ExportReportButton';

interface DonorsContactedChartProps {
  /** Supplied by ReportFilters (next issue). Defaults to the last 30 days until then. */
  filters?: DonorsContactedFilters;
}

type SortMode = 'count' | 'name';

function sortRows(rows: ManagerContacted[], mode: SortMode): ManagerContacted[] {
  const copy = [...rows];
  if (mode === 'name') {
    return copy.sort((a, b) => a.manager.fullName.localeCompare(b.manager.fullName));
  }
  // Highest first; the backend already returns this order, but we don't
  // depend on that staying true.
  return copy.sort(
    (a, b) => b.donorsContacted - a.donorsContacted || a.manager.fullName.localeCompare(b.manager.fullName),
  );
}

export function DonorsContactedChart({ filters = defaultDonorsContactedFilters() }: DonorsContactedChartProps) {
  const { data, isPending, isError, refetch } = useDonorsContactedReport(filters);
  const [sort, setSort] = useState<SortMode>('count');

  const rows = useMemo(() => (data ? sortRows(data.byManager, sort) : []), [data, sort]);
  const chartData = rows.map((r) => ({ name: r.manager.fullName, donorsContacted: r.donorsContacted }));
  const isEmpty = !data || data.byManager.length === 0 || data.totalDonorsContacted === 0;
  // Several managers → angle the labels so names don't collide.
  const angled = chartData.length > 5;

  return (
    <ChartSection
      title="Donors contacted"
      description="Per relationship manager"
      isLoading={isPending}
      isError={isError}
      isEmpty={isEmpty}
      emptyTitle="No contact activity yet"
      emptyMessage="No relationship manager has logged a donor interaction in this period."
      onRetry={() => refetch()}
      actions={
        <div className="flex items-center gap-2">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setSort((s) => (s === 'count' ? 'name' : 'count'))}
            aria-label={sort === 'count' ? 'Sort by name' : 'Sort by most contacted'}
          >
            {sort === 'count' ? 'Most contacted' : 'Name A–Z'}
          </Button>
          <ExportReportButton reportType="donors-contacted" filters={filters} />
        </div>
      }
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <BarChart data={chartData} margin={{ top: 8, right: 8, left: -16, bottom: angled ? 48 : 0 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
          <XAxis
            dataKey="name"
            tick={{ fontSize: 12, fill: 'var(--muted-c)' }}
            interval={0}
            angle={angled ? -35 : 0}
            textAnchor={angled ? 'end' : 'middle'}
          />
          <YAxis allowDecimals={false} tick={{ fontSize: 12, fill: 'var(--muted-c)' }} />
          <Tooltip cursor={{ fillOpacity: 0.1 }} formatter={(v) => [v, 'Donors contacted']} />
          <Bar dataKey="donorsContacted" fill="var(--brand)" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartSection>
  );
}
