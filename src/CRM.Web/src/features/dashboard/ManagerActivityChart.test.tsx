import { useState } from 'react';
import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { ManagerActivityChart } from './ManagerActivityChart';
import type { ManagerActivityPeriod } from './types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));

function respond(items: { userId: string; name: string; donorsContacted: number }[]) {
  vi.mocked(api.get).mockResolvedValue({
    data: { data: { period: 'weekly', fromUtc: '', toUtc: '', items } },
  });
}

/** The period is owned by DashboardPage; this stands in for it. */
function StatefulChart() {
  const [period, setPeriod] = useState<ManagerActivityPeriod>('weekly');
  return <ManagerActivityChart period={period} onPeriodChange={setPeriod} />;
}

function renderChart() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <StatefulChart />
    </QueryClientProvider>,
  );
}

describe('ManagerActivityChart', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders real totals and per-person bars from the API', async () => {
    respond([
      { userId: '1', name: 'Ada Lovelace', donorsContacted: 10 },
      { userId: '2', name: 'Grace Hopper', donorsContacted: 4 },
    ]);
    renderChart();

    expect(await screen.findByText('Ada')).toBeInTheDocument();
    expect(screen.getByText('Grace')).toBeInTheDocument();
    expect(screen.getByText('14')).toBeInTheDocument(); // total
    expect(screen.getByText('7')).toBeInTheDocument(); // average
    expect(api.get).toHaveBeenCalledWith('/api/v1/dashboard/manager-activity', { params: { period: 'weekly' } });
  });

  it('shows an empty state when nobody has contacted a donor', async () => {
    respond([]);
    renderChart();
    expect(await screen.findByText(/no donor contact has been logged/i)).toBeInTheDocument();
  });

  it('shows an error state when the request fails', async () => {
    vi.mocked(api.get).mockRejectedValue(new Error('boom'));
    renderChart();
    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });

  it('requests the monthly window when the period is toggled', async () => {
    respond([]);
    renderChart();
    await screen.findByText(/no donor contact/i);

    await userEvent.click(screen.getByRole('button', { name: /switch period/i }));

    await screen.findByText(/last 30 days/i, { selector: 'span' });
    expect(api.get).toHaveBeenLastCalledWith('/api/v1/dashboard/manager-activity', { params: { period: 'monthly' } });
  });
});
