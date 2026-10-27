import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuthStore } from '@/store/authStore';
import { Sidebar } from './Sidebar';

vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));

function renderAs(roles: string[]) {
  useAuthStore.setState({ user: { id: '1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles } } as Partial<
    ReturnType<typeof useAuthStore.getState>
  >);
  render(
    <QueryClientProvider client={new QueryClient()}>
      <Sidebar />
    </QueryClientProvider>,
  );
}

describe('Sidebar', () => {
  afterEach(cleanup);

  const SHARED_ACTIONS = ['Search', 'Saved filters', 'Calendar', 'Help & support'];

  it('shows every quick action, including Agent, to SuperAdmins', () => {
    renderAs(['SuperAdmin']);
    for (const name of SHARED_ACTIONS) expect(screen.getByRole('button', { name })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open CRM assistant' })).toBeInTheDocument();
  });

  it.each(['Admin', 'Procurement', 'Marketing'])('shows the rail without Agent to %s', (role) => {
    renderAs([role]);
    for (const name of SHARED_ACTIONS) expect(screen.getByRole('button', { name })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Open CRM assistant' })).not.toBeInTheDocument();
  });
});
