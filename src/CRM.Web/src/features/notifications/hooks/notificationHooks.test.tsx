import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { useNotifications } from './useNotifications';
import { useMarkNotificationRead } from './useMarkNotificationRead';
import { useMarkAllNotificationsRead } from './useMarkAllNotificationsRead';
import { notificationKeys } from './notificationKeys';
import type { NotificationListDto } from '../types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), patch: vi.fn() } }));

const list: NotificationListDto = {
  data: [
    {
      id: 'n1',
      title: 'New task assigned',
      message: 'Confirm Thursday collection slot',
      isRead: false,
      readAt: null,
      notificationType: 'TaskAssigned',
      relatedEntityId: 'task-1',
      relatedEntityType: 'DonorTask',
      createdAt: '2026-09-06T09:00:00Z',
    },
  ],
  pagination: { page: 1, pageSize: 12, totalCount: 1, totalPages: 1 },
  unreadCount: 3,
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
  };
}

describe('notification hooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ isAuthenticated: true });
  });

  it('useNotifications → GETs /api/v1/notifications and exposes unreadCount', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: list });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useNotifications({ pageSize: 12 }), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.get).toHaveBeenCalledWith('/api/v1/notifications', { params: { pageSize: 12 } });
    expect(result.current.data?.unreadCount).toBe(3);
  });

  it('useNotifications → stays idle when not authenticated', () => {
    useAuthStore.setState({ isAuthenticated: false });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useNotifications(), { wrapper });
    expect(result.current.fetchStatus).toBe('idle');
    expect(api.get).not.toHaveBeenCalled();
  });

  it('useMarkNotificationRead → PATCHes /{id}/read and invalidates the bell', async () => {
    vi.mocked(api.patch).mockResolvedValueOnce({ status: 204 });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useMarkNotificationRead(), { wrapper });

    result.current.mutate('n1');
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.patch).toHaveBeenCalledWith('/api/v1/notifications/n1/read');
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: notificationKeys.all });
  });

  it('useMarkAllNotificationsRead → PATCHes /read-all', async () => {
    vi.mocked(api.patch).mockResolvedValueOnce({ status: 204 });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useMarkAllNotificationsRead(), { wrapper });

    result.current.mutate();
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.patch).toHaveBeenCalledWith('/api/v1/notifications/read-all');
  });
});
