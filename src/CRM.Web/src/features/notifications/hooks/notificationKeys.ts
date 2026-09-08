import type { NotificationFilters } from '../types';

/**
 * Query-key factory for the header bell. `notificationKeys.all` invalidates every
 * page/filter variation at once — used after mark-read / mark-all-read.
 */
export const notificationKeys = {
  all: ['notifications'] as const,
  list: (filters: NotificationFilters = {}) => [...notificationKeys.all, 'list', filters] as const,
};
