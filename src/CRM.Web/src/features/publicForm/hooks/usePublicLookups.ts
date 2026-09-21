import { useQuery } from '@tanstack/react-query';
import { publicApi } from '@/lib/publicApi';
import { publicLookupKeys } from './publicLookupKeys';
import type { LookupDto, ProvinceDto, RegionDto } from '@/features/donors/types';

// Same reasoning as features/lookups/useLookups.ts: this reference data is
// admin-managed and changes essentially never, so it's cached for a full day
// rather than refetched per mount. gcTime matches staleTime for the same
// reason documented there.
const STALE_TIME = 24 * 60 * 60 * 1000;
const GC_TIME = STALE_TIME;

export function usePublicCompanyTypes() {
  return useQuery<LookupDto[]>({
    queryKey: publicLookupKeys.companyTypes(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: LookupDto[] }>('/api/v1/public/lookups/company-types');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function usePublicEntityTypes() {
  return useQuery<LookupDto[]>({
    queryKey: publicLookupKeys.entityTypes(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: LookupDto[] }>('/api/v1/public/lookups/entity-types');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function usePublicOperationalRegions() {
  return useQuery<RegionDto[]>({
    queryKey: publicLookupKeys.operationalRegions(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: RegionDto[] }>('/api/v1/public/lookups/operational-regions');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function usePublicDonationTypes() {
  return useQuery<LookupDto[]>({
    queryKey: publicLookupKeys.donationTypes(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: LookupDto[] }>('/api/v1/public/lookups/donation-types');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function usePublicDonationFrequencies() {
  return useQuery<LookupDto[]>({
    queryKey: publicLookupKeys.donationFrequencies(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: LookupDto[] }>('/api/v1/public/lookups/donation-frequencies');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function usePublicProvinces() {
  return useQuery<ProvinceDto[]>({
    queryKey: publicLookupKeys.provinces(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: ProvinceDto[] }>('/api/v1/public/lookups/provinces');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}

export function usePublicBbbeeStatuses() {
  return useQuery<LookupDto[]>({
    queryKey: publicLookupKeys.bbbeeStatuses(),
    queryFn: async () => {
      const { data } = await publicApi.get<{ data: LookupDto[] }>('/api/v1/public/lookups/bbbee-statuses');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: GC_TIME,
  });
}
