import type { DonorsContactedFilters } from '../types';

/**
 * Single source of truth for report query keys, mirroring donorKeys.ts's
 * factory pattern so cache entries are consistent and easy to invalidate.
 */
export const reportKeys = {
  all: ['reports'] as const,
  donorsContacted: (filters: DonorsContactedFilters) => [...reportKeys.all, 'donors-contacted', filters] as const,
  donorsByRegion: () => [...reportKeys.all, 'donors-by-region'] as const,
  donorsByType: () => [...reportKeys.all, 'donors-by-type'] as const,
  donorsByStatus: () => [...reportKeys.all, 'donors-by-status'] as const,
};
