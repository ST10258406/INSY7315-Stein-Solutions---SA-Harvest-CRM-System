import type { DonorFilters } from '../types';

/**
 * Single source of truth for donor query keys. All donor hooks build their keys
 * through this factory instead of ad-hoc arrays, so invalidation targets the
 * right cache entries consistently (e.g. `donorKeys.lists()` invalidates every
 * filter/page variation of the list at once).
 */
export const donorKeys = {
  all: ['donors'] as const,
  lists: () => [...donorKeys.all, 'list'] as const,
  list: (filters: DonorFilters = {}) => [...donorKeys.lists(), filters] as const,
  details: () => [...donorKeys.all, 'detail'] as const,
  detail: (id: string | undefined) => [...donorKeys.details(), id] as const,
};
