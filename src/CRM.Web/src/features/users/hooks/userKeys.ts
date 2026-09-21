import type { UserFilters } from '../types';

/**
 * Single source of truth for user query keys, mirroring donorKeys. All user
 * hooks build their keys through this factory so invalidation targets the
 * right cache entries consistently.
 */
export const userKeys = {
  all: ['users'] as const,
  lists: () => [...userKeys.all, 'list'] as const,
  list: (filters: UserFilters = {}) => [...userKeys.lists(), filters] as const,
  roles: () => [...userKeys.all, 'roles'] as const,
};
