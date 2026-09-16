import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { StatsCards } from './StatsCards';
import type { DashboardStatsDto } from './types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));

const STATS: DashboardStatsDto = {
  totalDonors: 42,
  activeDonors: 30,
  pendingApprovals: 3,
  myOpenTasks: 5,
  myOverdueFollowUps: 2,
  donorsContactedThisMonth: 12,
  donorsContactedLastMonth: 9,
};

function setRoles(roles: string[]) {
  useAuthStore.setState({ user: { id: 'u1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles } });
}

function renderCards() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <StatsCards />
    </QueryClientProvider>,
  );
}

describe('StatsCards', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders all four cards with real values from GET /dashboard/stats for an Admin', async () => {
    setRoles(['Admin']);
    vi.mocked(api.get).mockResolvedValue({ data: { data: STATS } });

    renderCards();

    expect(await screen.findByText('Pending Approvals')).toBeInTheDocument();
    expect(await screen.findByText('3')).toBeInTheDocument();
    expect(screen.getByText('My Open Tasks')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('My Overdue Follow-Ups')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('Donors Contacted This Month')).toBeInTheDocument();
    expect(screen.getByText('12')).toBeInTheDocument();
  });

  it('hides the Pending Approvals card entirely for a non-Admin, even though the field is present as 0', async () => {
    setRoles(['Procurement']);
    // Mirrors the real backend contract: pendingApprovals is forced to 0 for non-Admins,
    // it is not omitted from the payload.
    vi.mocked(api.get).mockResolvedValue({ data: { data: { ...STATS, pendingApprovals: 0 } } });

    renderCards();

    expect(await screen.findByText('My Open Tasks')).toBeInTheDocument();
    expect(screen.queryByText('Pending Approvals')).not.toBeInTheDocument();
  });

  it('shows a loading skeleton (no leftover static numbers) while the request is in flight', () => {
    setRoles(['Admin']);
    vi.mocked(api.get).mockReturnValue(new Promise(() => {}));

    const { container } = renderCards();

    expect(container.querySelectorAll('.animate-pulse').length).toBe(4);
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it("shows a couldn't-load placeholder per card on error, not a crash or a stale zero", async () => {
    setRoles(['Admin']);
    vi.mocked(api.get).mockRejectedValue(new Error('network error'));

    renderCards();

    expect((await screen.findAllByTitle("Couldn't load this count")).length).toBe(4);
  });
});
