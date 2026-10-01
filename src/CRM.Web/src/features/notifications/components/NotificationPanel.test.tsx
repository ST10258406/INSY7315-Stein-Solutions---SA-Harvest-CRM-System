import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { NotificationPanel } from './NotificationPanel';
import type { NotificationListDto } from '../types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), patch: vi.fn() } }));

const list: NotificationListDto = {
  data: [
    {
      id: 'n1',
      title: 'New task assigned',
      message: 'Confirm Thursday collection slot with FoodCorp SA',
      isRead: false,
      readAt: null,
      notificationType: 'TaskAssigned',
      relatedEntityId: 'task-1',
      relatedEntityType: 'DonorTask',
      createdAt: '2026-09-06T09:00:00Z',
    },
  ],
  pagination: { page: 1, pageSize: 12, totalCount: 1, totalPages: 1 },
  unreadCount: 2,
};

function renderPanel(onClose = vi.fn()) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route path="/dashboard" element={<NotificationPanel onClose={onClose} />} />
          <Route path="/tasks" element={<div data-testid="tasks-page">Tasks</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
  return { onClose };
}

describe('NotificationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ isAuthenticated: true });
  });
  afterEach(cleanup);

  it('lists notifications and marks all read', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: list });
    vi.mocked(api.patch).mockResolvedValue({ status: 204 });
    renderPanel();

    expect(await screen.findByText('New task assigned')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /mark all read/i }));
    expect(api.patch).toHaveBeenCalledWith('/api/v1/notifications/read-all');
  });

  it('activating an unread notification marks it read and closes the panel', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: list });
    vi.mocked(api.patch).mockResolvedValue({ status: 204 });
    const { onClose } = renderPanel();

    await userEvent.click(await screen.findByText('New task assigned'));

    expect(api.patch).toHaveBeenCalledWith('/api/v1/notifications/n1/read');
    expect(onClose).toHaveBeenCalled();
  });

  it('shows the empty state when there are no notifications', async () => {
    vi.mocked(api.get).mockResolvedValue({
      data: { data: [], pagination: { page: 1, pageSize: 12, totalCount: 0, totalPages: 0 }, unreadCount: 0 },
    });
    renderPanel();
    expect(await screen.findByText(/all caught up/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /mark all read/i })).toBeDisabled();
  });
});
