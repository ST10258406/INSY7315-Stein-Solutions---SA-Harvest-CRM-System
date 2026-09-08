import { render, screen, cleanup } from '@testing-library/react';
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
    isDevBypass: false,
  });
}

describe('TopBar nav — Approvals link RoleGuard (Issue #113)', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('hides Approvals (and Users) from a non-admin role', () => {
    signInAs(['Procurement']);
    renderTopBar();

    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /donors/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /tasks/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /reports/i })).toBeInTheDocument();

    expect(screen.queryByRole('link', { name: /approvals/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /users/i })).not.toBeInTheDocument();
  });

  it('shows Approvals to an Admin', () => {
    signInAs(['Admin']);
    renderTopBar();
    expect(screen.getByRole('link', { name: /approvals/i })).toHaveAttribute('href', '/approvals');
  });

  it('shows Approvals to a SuperAdmin', () => {
    signInAs(['SuperAdmin']);
    renderTopBar();
    expect(screen.getByRole('link', { name: /approvals/i })).toBeInTheDocument();
  });
});
