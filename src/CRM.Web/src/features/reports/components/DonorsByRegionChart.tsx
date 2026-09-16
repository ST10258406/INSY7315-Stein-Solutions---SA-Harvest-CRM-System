import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { useDonorsByRegionReport } from '../hooks/useDonorsByRegionReport';
import { CHART_HEIGHT, ChartSection } from './ChartSection';

/** Horizontal bars: region names are longer than a Y-axis count needs to be legible on. */
export function DonorsByRegionChart() {
  const { data, isPending, isError, refetch } = useDonorsByRegionReport();
  // Every active region is always present (zero-filled by the backend), so
  // "empty" means every count is zero, not that the list is short.
  const isEmpty = !data || data.every((r) => r.donorCount === 0);

  return (
    <ChartSection
      title="Donors by region"
      description="All time"
      isLoading={isPending}
      isError={isError}
      isEmpty={isEmpty}
      emptyTitle="No donors yet"
      emptyMessage="Once donors are added, you'll see how they're spread across regions."
      onRetry={() => refetch()}
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <BarChart data={data} layout="vertical" margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="var(--border)" />
          <XAxis type="number" allowDecimals={false} tick={{ fontSize: 12, fill: 'var(--muted-c)' }} />
          <YAxis
            type="category"
            dataKey="regionName"
            width={110}
            interval={0}
            tick={{ fontSize: 12, fill: 'var(--muted-c)' }}
          />
          <Tooltip cursor={{ fillOpacity: 0.1 }} formatter={(v) => [v, 'Donors']} />
          <Bar dataKey="donorCount" fill="var(--brand)" radius={[0, 4, 4, 0]} minPointSize={0} />
        </BarChart>
      </ResponsiveContainer>
    </ChartSection>
  );
}
