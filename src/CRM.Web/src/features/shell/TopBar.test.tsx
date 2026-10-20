import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { TopBar } from './TopBar';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn().mockResolvedValue({ data: null }), post: vi.fn() } }));

function renderTopBar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/dashboard']}>
        <TopBar />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function signInAs(roles: string[]) {
  useAuthStore.setState({
    user: { id: 'u1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@saharvest.org', roles },
    isAuthenticated: true,
    isHydrating: false,
  });
}

describe('TopBar nav — Approvals/Reports link RoleGuard (Issues #113, #73)', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('hides Approvals, Reports, and Users from a non-admin role', () => {
    signInAs(['Procurement']);
    renderTopBar();

    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /donors/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /tasks/i })).toBeInTheDocument();

    expect(screen.queryByRole('link', { name: /approvals/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /reports/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /users/i })).not.toBeInTheDocument();
  });

  it('shows Approvals and Reports to an Admin', () => {
    signInAs(['Admin']);
    renderTopBar();
    expect(screen.getByRole('link', { name: /approvals/i })).toHaveAttribute('href', '/approvals');
    expect(screen.getByRole('link', { name: /reports/i })).toHaveAttribute('href', '/reports');
  });

  it('shows Approvals and Reports to a SuperAdmin', () => {
    signInAs(['SuperAdmin']);
    renderTopBar();
    expect(screen.getByRole('link', { name: /approvals/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /reports/i })).toBeInTheDocument();
  });
});

describe('TopBar nav — Vetting menu RoleGuard (Issue #160)', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  const vettingTrigger = () => screen.queryByRole('button', { name: /vetting/i });

  it('hides Vetting from Marketing', () => {
    signInAs(['Marketing']);
    renderTopBar();
    expect(vettingTrigger()).not.toBeInTheDocument();
  });

  it('shows Procurement Submissions and Site visits, but not the Admin-only links', async () => {
    signInAs(['Procurement']);
    renderTopBar();
    await userEvent.setup().click(vettingTrigger()!);

    expect(await screen.findByRole('menuitem', { name: 'Submissions' })).toHaveAttribute('href', '/vetting');
    expect(screen.getByRole('menuitem', { name: 'Site visits' })).toHaveAttribute('href', '/vetting/visits');
    expect(screen.queryByRole('menuitem', { name: 'Review queue' })).not.toBeInTheDocument();
    expect(screen.queryByRole('menuitem', { name: 'Officers' })).not.toBeInTheDocument();
  });

  it.each(['Admin', 'SuperAdmin'])('shows %s the Review queue and Officers links', async (role) => {
    signInAs([role]);
    renderTopBar();
    await userEvent.setup().click(vettingTrigger()!);

    expect(await screen.findByRole('menuitem', { name: 'Review queue' })).toHaveAttribute('href', '/vetting/review');
    expect(screen.getByRole('menuitem', { name: 'Officers' })).toHaveAttribute('href', '/vetting/officers');
  });
});
