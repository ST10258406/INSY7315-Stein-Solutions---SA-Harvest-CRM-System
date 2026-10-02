import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { lookupKeys } from './lookupKeys';
import type { LookupDto, ProvinceDto, RegionDto, RelationshipManagerDto } from '@/features/donors/types';

// Reference data changes rarely (admin-managed lookup tables) — per the
// project's caching strategy this is fetched once on login and shared across
// every component via the query cache for a full day, not refetched per
// mount. gcTime must match staleTime: React Query's default gcTime (5 min)
// would otherwise evict the cache entry once every consumer unmounts for
// 5+ minutes, silently undermining the staleTime and forcing a refetch on
// the next mount regardless.
const STALE_TIME = 24 * 60 * 60 * 1000;
const GC_TIME = STALE_TIME;

export function useCompanyTypes() {
  return useQuery<LookupDto[]>({
    queryKey: lookupKeys.companyTypes(),
    queryFn: async () => {
      const { data } = await api.get<{ data: LookupDto[] }>('/api/v1/lookups/company-types');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function useOperationalRegions() {
  return useQuery<RegionDto[]>({
    queryKey: lookupKeys.operationalRegions(),
    queryFn: async () => {
      const { data } = await api.get<{ data: RegionDto[] }>('/api/v1/lookups/operational-regions');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function useDonationTypes() {
  return useQuery<LookupDto[]>({
    queryKey: lookupKeys.donationTypes(),
    queryFn: async () => {
      const { data } = await api.get<{ data: LookupDto[] }>('/api/v1/lookups/donation-types');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function useDonationFrequencies() {
  return useQuery<LookupDto[]>({
    queryKey: lookupKeys.donationFrequencies(),
    queryFn: async () => {
      const { data } = await api.get<{ data: LookupDto[] }>('/api/v1/lookups/donation-frequencies');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function useEntityTypes() {
  return useQuery<LookupDto[]>({
    queryKey: lookupKeys.entityTypes(),
    queryFn: async () => {
      const { data } = await api.get<{ data: LookupDto[] }>('/api/v1/lookups/entity-types');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function useProvinces() {
  return useQuery<ProvinceDto[]>({
    queryKey: lookupKeys.provinces(),
    queryFn: async () => {
      const { data } = await api.get<{ data: ProvinceDto[] }>('/api/v1/lookups/provinces');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function useBbbeeStatuses() {
  return useQuery<LookupDto[]>({
    queryKey: lookupKeys.bbbeeStatuses(),
    queryFn: async () => {
      const { data } = await api.get<{ data: LookupDto[] }>('/api/v1/lookups/bbbee-statuses');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

/** Active relationship managers (users holding the Procurement role) — used by the reports
 * "Relationship manager" filter. Not part of `useDonorLookups`: nothing on the donor form
 * consumes it yet. */
export function useRelationshipManagers() {
  return useQuery<RelationshipManagerDto[]>({
    queryKey: lookupKeys.relationshipManagers(),
    queryFn: async () => {
      const { data } = await api.get<{ data: RelationshipManagerDto[] }>('/api/v1/lookups/relationship-managers');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export interface DonorLookups {
  companyTypes: ReturnType<typeof useCompanyTypes>;
  entityTypes: ReturnType<typeof useEntityTypes>;
  operationalRegions: ReturnType<typeof useOperationalRegions>;
  donationTypes: ReturnType<typeof useDonationTypes>;
  donationFrequencies: ReturnType<typeof useDonationFrequencies>;
  provinces: ReturnType<typeof useProvinces>;
  bbbeeStatuses: ReturnType<typeof useBbbeeStatuses>;
}

/**
 * All 7 donor lookups in one call. Each still hits its own endpoint (there's
 * no combined backend route) and still dedupes by query key like any other
 * useQuery consumer, but calling this once near the top of a page/flow warms
 * every lookup together instead of each dropdown being the first thing to
 * trigger its own fetch as it happens to mount.
 */
export function useDonorLookups(): DonorLookups {
  return {
    companyTypes: useCompanyTypes(),
    entityTypes: useEntityTypes(),
    operationalRegions: useOperationalRegions(),
    donationTypes: useDonationTypes(),
    donationFrequencies: useDonationFrequencies(),
    provinces: useProvinces(),
    bbbeeStatuses: useBbbeeStatuses(),
  };
}
