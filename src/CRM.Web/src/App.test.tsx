import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useAuthStore } from '@/store/authStore';
import App from './App';

// No refresh cookie in tests — hydration finds no session and leaves signInAs's state alone.
vi.mock('@/lib/axios', () => ({
  api: { get: vi.fn().mockResolvedValue({ data: null }), post: vi.fn() },
  refreshSession: vi.fn().mockRejectedValue(new Error('no session')),
}));

// ReportsPage composes several chart hooks that each hit the API — irrelevant to what
// this suite verifies (routing/role enforcement), so it's swapped for a stub.
vi.mock('./components/pages/ReportsPage', () => ({
  default: () => <div data-testid="reports-page">Reports</div>,
}));

function signInAs(roles: string[]) {
  useAuthStore.setState({
    user: { id: 'u1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@saharvest.org', roles },
    isAuthenticated: true,
    isHydrating: false,
  });
}

describe('App routing — Reports is Admin-only (Issue #73)', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(() => {
    cleanup();
    window.history.pushState({}, '', '/');
  });

  it('blocks a non-Admin navigating directly to /reports by URL, redirecting to /not-authorized', async () => {
    signInAs(['Procurement']);
    window.history.pushState({}, '', '/reports');

    render(<App />);

    expect(await screen.findByText(/don't have access to this page/i)).toBeInTheDocument();
    expect(screen.queryByTestId('reports-page')).not.toBeInTheDocument();
  });

  it('lets an Admin navigating directly to /reports by URL through to the page', async () => {
    signInAs(['Admin']);
    window.history.pushState({}, '', '/reports');

    render(<App />);

    expect(await screen.findByTestId('reports-page')).toBeInTheDocument();
  });

  it('lets a SuperAdmin navigating directly to /reports by URL through to the page', async () => {
    signInAs(['SuperAdmin']);
    window.history.pushState({}, '', '/reports');

    render(<App />);

    expect(await screen.findByTestId('reports-page')).toBeInTheDocument();
  });
});
