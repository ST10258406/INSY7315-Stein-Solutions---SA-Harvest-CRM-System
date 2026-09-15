import { useMemo } from 'react';
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { useDonorsByStatusReport } from '../hooks/useDonorsByStatusReport';
import { sortByStatusOrder, statusColor, statusLabel } from '../lib/statusColors';
import { CHART_HEIGHT, ChartSection } from './ChartSection';

export function DonorsByStatusChart() {
  const { data, isPending, isError, refetch } = useDonorsByStatusReport();

  // Fixed slice order + a colour keyed by status name (never by array index),
  // so a slice's colour can't reshuffle if the response order ever changes.
  const rows = useMemo(
    () => (data ? sortByStatusOrder(data).map((r) => ({ ...r, label: statusLabel(r.status) })) : []),
    [data],
  );
  const isEmpty = !data || data.every((r) => r.donorCount === 0);

  return (
    <ChartSection
      title="Donors by status"
      description="All time"
      isLoading={isPending}
      isError={isError}
      isEmpty={isEmpty}
      emptyTitle="No donors yet"
      emptyMessage="Once donors are added, you'll see how many are active, pending, lapsed or rejected."
      onRetry={() => refetch()}
    >
      <ResponsiveContainer width="100%" height={CHART_HEIGHT}>
        <PieChart>
          <Pie data={rows} dataKey="donorCount" nameKey="label" innerRadius="45%" outerRadius="75%" paddingAngle={1}>
            {rows.map((r) => (
              <Cell key={r.status} fill={statusColor(r.status)} />
            ))}
          </Pie>
          <Tooltip formatter={(v, name) => [v, name]} />
          <Legend verticalAlign="bottom" iconType="circle" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </ChartSection>
  );
}
