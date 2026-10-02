import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { userKeys } from './userKeys';
import type { RoleDto } from '../types';

// Reference data (the four seeded roles) changes essentially never — cache it
// for a full day like the other lookup dropdowns (see useLookups.ts).
const STALE_TIME = 24 * 60 * 60 * 1000;

export function useRoles() {
  return useQuery<RoleDto[]>({
    queryKey: userKeys.roles(),
    queryFn: async () => {
      const { data } = await api.get<{ data: RoleDto[] }>('/api/v1/lookups/roles');
      return data.data;
    },
    staleTime: STALE_TIME,
    gcTime: STALE_TIME,
  });
}
