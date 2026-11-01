import { render, screen, cleanup, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { Sidebar } from './Sidebar';

vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));

function renderAs(roles: string[]) {
  useAuthStore.setState({ user: { id: '1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles } } as Partial<
    ReturnType<typeof useAuthStore.getState>
  >);
  render(
    <MemoryRouter>
      <QueryClientProvider client={new QueryClient()}>
        <Sidebar />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('Sidebar', () => {
  afterEach(cleanup);

  const SHARED_ACTIONS = ['Open quick search (Ctrl+K)', 'Open calendar', 'Open help and support'];

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

  it.each([
    ['SuperAdmin', true],
    ['Admin', true],
    ['Procurement', true],
    ['Marketing', false],
  ])('shows Saved filters to %s: %s', (role, visible) => {
    renderAs([role as string]);
    expect(!!screen.queryByRole('button', { name: 'Saved filters' })).toBe(visible);
  });

  it('opens quick search from the rail button and from Ctrl+K', async () => {
    renderAs(['Procurement']);
    fireEvent.click(screen.getByRole('button', { name: 'Open quick search (Ctrl+K)' }));
    expect(await screen.findByRole('dialog', { name: 'Quick search' })).toBeInTheDocument();
    cleanup();

    renderAs(['Procurement']);
    fireEvent.keyDown(window, { key: 'k', ctrlKey: true });
    expect(await screen.findByRole('dialog', { name: 'Quick search' })).toBeInTheDocument();
  });

  it('opens quick search with Ctrl+K while the rail is collapsed', async () => {
    renderAs(['Procurement']);
    fireEvent.click(screen.getByTitle('Collapse rail'));
    fireEvent.keyDown(window, { key: 'K', metaKey: true });
    expect(await screen.findByRole('dialog', { name: 'Quick search' })).toBeInTheDocument();
  });

  it('closes quick search on Esc', async () => {
    renderAs(['Procurement']);
    fireEvent.keyDown(window, { key: 'k', ctrlKey: true });
    fireEvent.keyDown(await screen.findByRole('combobox', { name: 'Search' }), { key: 'Escape' });
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Quick search' })).not.toBeInTheDocument());
  });
});
