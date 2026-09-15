export const lookupKeys = {
  all: ['lookups'] as const,
  companyTypes: () => [...lookupKeys.all, 'company-types'] as const,
  entityTypes: () => [...lookupKeys.all, 'entity-types'] as const,
  operationalRegions: () => [...lookupKeys.all, 'operational-regions'] as const,
  donationTypes: () => [...lookupKeys.all, 'donation-types'] as const,
  donationFrequencies: () => [...lookupKeys.all, 'donation-frequencies'] as const,
  provinces: () => [...lookupKeys.all, 'provinces'] as const,
  bbbeeStatuses: () => [...lookupKeys.all, 'bbbee-statuses'] as const,
  relationshipManagers: () => [...lookupKeys.all, 'relationship-managers'] as const,
};
