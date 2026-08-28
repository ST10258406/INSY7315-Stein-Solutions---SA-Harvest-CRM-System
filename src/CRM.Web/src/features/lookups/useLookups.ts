import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { lookupKeys } from './lookupKeys';
import type { LookupDto, ProvinceDto, RegionDto } from '@/features/donors/types';

// Reference data changes rarely (admin-managed lookup tables) — cache it for the
// session instead of refetching on every donor list/filter mount.
const STALE_TIME = 5 * 60 * 1000;

export function useCompanyTypes() {
  return useQuery<LookupDto[]>({
    queryKey: lookupKeys.companyTypes(),
    queryFn: async () => {
      const { data } = await api.get<{ data: LookupDto[] }>('/api/v1/lookups/company-types');
      return data.data;
    },
    staleTime: STALE_TIME,
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
  });
}
