import type { InteractionFilters } from '../types';

/**
 * Query-key factory for a donor's interaction timeline. Interactions are always
 * donor-scoped (there is no global list), so every key is namespaced by donorId.
 * `interactionKeys.donor(id)` invalidates every filter/page variation at once.
 */
export const interactionKeys = {
  all: ['interactions'] as const,
  donor: (donorId: string | undefined) => [...interactionKeys.all, donorId] as const,
  list: (donorId: string | undefined, filters: Omit<InteractionFilters, 'page'> = {}) =>
    [...interactionKeys.donor(donorId), 'list', filters] as const,
};
