import type { DonorTasksFilters, MyTasksFilters } from '../types';

/**
 * Query-key factory for tasks. Two read surfaces share the `['tasks']` root so a
 * single `taskKeys.all` invalidation refreshes both "My Tasks" and every donor's
 * task list after a create / update / complete / reopen.
 */
export const taskKeys = {
  all: ['tasks'] as const,
  mine: (filters: MyTasksFilters = {}) => [...taskKeys.all, 'mine', filters] as const,
  donor: (donorId: string | undefined, filters: DonorTasksFilters = {}) =>
    [...taskKeys.all, 'donor', donorId, filters] as const,
};
