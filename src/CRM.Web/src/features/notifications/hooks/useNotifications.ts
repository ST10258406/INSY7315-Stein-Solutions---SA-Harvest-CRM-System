import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { notificationKeys } from './notificationKeys';
import type { ApiError, NotificationFilters, NotificationListDto } from '../types';

/**
 * GET /api/v1/notifications — the current user's notifications, newest first.
 * `staleTime: 0` + refetch on window focus so the bell badge stays close to
 * live without polling. Returns `{ data, pagination, unreadCount }` directly.
 */
export function useNotifications(filters: NotificationFilters = {}) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const isDevBypass = useAuthStore((s) => s.isDevBypass);

  return useQuery<NotificationListDto, ApiError>({
    queryKey: notificationKeys.list(filters),
    // The dev landing-page bypass has no real token, so every call 401s.
    enabled: isAuthenticated && !isDevBypass,
    queryFn: async () => {
      const { data } = await api.get<NotificationListDto>('/api/v1/notifications', { params: filters });
      return data;
    },
    staleTime: 0,
    refetchOnWindowFocus: true,
    placeholderData: keepPreviousData,
  });
}
