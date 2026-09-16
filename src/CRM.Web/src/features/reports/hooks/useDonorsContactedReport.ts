import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { reportKeys } from './reportKeys';
import { thisCalendarMonthRange } from '../lib/date';
import type { DonorsContactedFilters, DonorsContactedReport } from '../types';

/**
 * The backend requires startDate/endDate (GetDonorsContactedReportQueryValidator rejects a
 * request without both). ReportFilters uses this as its initial range on first load — "this
 * calendar month" so the report isn't empty by default — and it's also the fallback for any
 * caller that renders the chart without filters.
 */
export function defaultDonorsContactedFilters(): DonorsContactedFilters {
  return thisCalendarMonthRange();
}

/** The only report scoped to a date range — ReportFilters (rendered above the chart) owns and
 * supplies `filters`; the other three report charts are always all-time and unfiltered. */
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
