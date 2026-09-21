// Kept separate from features/lookups/lookupKeys.ts — same query shapes, but
// a distinct cache namespace since these are fetched over `publicApi`
// (no auth header) rather than the authenticated `api` client.
export const publicLookupKeys = {
  all: ['public-lookups'] as const,
  companyTypes: () => [...publicLookupKeys.all, 'company-types'] as const,
  entityTypes: () => [...publicLookupKeys.all, 'entity-types'] as const,
  operationalRegions: () => [...publicLookupKeys.all, 'operational-regions'] as const,
  donationTypes: () => [...publicLookupKeys.all, 'donation-types'] as const,
  donationFrequencies: () => [...publicLookupKeys.all, 'donation-frequencies'] as const,
  provinces: () => [...publicLookupKeys.all, 'provinces'] as const,
  bbbeeStatuses: () => [...publicLookupKeys.all, 'bbbee-statuses'] as const,
};
