import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { downloadCsv, todayYmd } from '@/lib/csv';
import { buildDonorListCsv } from '../lib/donorCsv';
import type { ApiError, DonorFilters, DonorListItemDto } from '../types';
import { fetchDonors } from './useDonors';

// GetDonorsQueryValidator caps PageSize at 100, so the export pages through the list.
const EXPORT_PAGE_SIZE = 100;
// Safety stop (10 000 donors) so a runaway loop can't hammer the API.
const MAX_PAGES = 100;

/**
 * Downloads every donor matching the list page's current search/filters/sort as a
 * CSV — not just the visible page. Built client-side from GET /api/v1/donors
 * (there is no server-side donor export), so it carries exactly what the caller
 * is already authorised to list. A mutation: it has a side effect and nothing to cache.
 */
export function useExportDonors() {
  return useMutation<number, ApiError, DonorFilters>({
    mutationFn: async (filters) => {
      const base: DonorFilters = { ...filters, page: 1, pageSize: EXPORT_PAGE_SIZE };
      const first = await fetchDonors(base);
      const donors: DonorListItemDto[] = [...first.data];
      const totalPages = Math.min(first.pagination.totalPages, MAX_PAGES);

      for (let page = 2; page <= totalPages; page++) {
        const next = await fetchDonors({ ...base, page });
        donors.push(...next.data);
      }

      downloadCsv(buildDonorListCsv(donors), `sa-harvest-donors-${todayYmd()}.csv`);
      return donors.length;
    },
    onSuccess: (count) => {
      toast.success(`Exported ${count} donor${count === 1 ? '' : 's'}.`);
    },
    onError: (error) => {
      toast.error(error.response?.data?.message ?? 'Export failed. Please try again.');
    },
  });
}
