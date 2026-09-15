import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { reportKeys } from './reportKeys';
import type { DonorsContactedFilters, DonorsContactedReport } from '../types';

/**
 * The backend requires startDate/endDate (GetDonorsContactedReportQueryValidator
 * rejects a request without both), so the hook needs a default until
 * ReportFilters (next issue) is wired up to supply one. Defaults to the
 * last 30 days, inclusive of today.
 */
export function defaultDonorsContactedFilters(): DonorsContactedFilters {
  const toYmd = (d: Date) =>
    `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  const end = new Date();
  const start = new Date();
  start.setDate(start.getDate() - 29);
  return { startDate: toYmd(start), endDate: toYmd(end) };
}

/** The only report scoped to a date range — pass `filters` once ReportFilters exists. */
export function useDonorsContactedReport(filters: DonorsContactedFilters = defaultDonorsContactedFilters()) {
  return useQuery({
    queryKey: reportKeys.donorsContacted(filters),
    queryFn: async () => {
      const { data } = await api.get<{ data: DonorsContactedReport }>('/api/v1/reports/donors-contacted', {
        params: filters,
      });
      return data.data;
    },
    // Keep the current bars on screen while a new date range loads, instead
    // of flashing back to the skeleton.
    placeholderData: keepPreviousData,
  });
}
