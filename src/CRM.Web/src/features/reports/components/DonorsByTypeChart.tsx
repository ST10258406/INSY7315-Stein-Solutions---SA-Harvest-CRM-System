import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { useDonorsByTypeReport } from '../hooks/useDonorsByTypeReport';
import { CHART_HEIGHT, ChartSection } from './ChartSection';
import { ExportReportButton } from './ExportReportButton';

export function DonorsByTypeChart() {
  const { data, isPending, isError, refetch } = useDonorsByTypeReport();
  // Every active donation type is always present (zero-filled by the backend).
  const isEmpty = !data || data.every((r) => r.donorCount === 0);
  const angled = (data?.length ?? 0) > 5;

  return (
    <ChartSection
      title="Donors by donation type"
      description="All time"
      isLoading={isPending}
      isError={isError}
      isEmpty={isEmpty}
      emptyTitle="No donors yet"
      emptyMessage="Once donors are added, you'll see which donation types they give."
      onRetry={() => refetch()}
      actions={<ExportReportButton reportType="donors-by-type" />}
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <BarChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: angled ? 48 : 0 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--border)" />
          <XAxis
            dataKey="donationType"
            interval={0}
            tick={{ fontSize: 12, fill: 'var(--muted-c)' }}
            angle={angled ? -35 : 0}
            textAnchor={angled ? 'end' : 'middle'}
          />
          <YAxis allowDecimals={false} tick={{ fontSize: 12, fill: 'var(--muted-c)' }} />
          <Tooltip cursor={{ fillOpacity: 0.1 }} formatter={(v) => [v, 'Donors']} />
          <Bar dataKey="donorCount" fill="var(--brand)" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartSection>
  );
}
