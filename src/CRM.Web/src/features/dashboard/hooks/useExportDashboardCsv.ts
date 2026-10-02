import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { donorKeys, fetchDonors } from '@/features/donors/hooks';
import type { ApiError, DonorFilters } from '@/features/donors/types';
import { buildDashboardCsv } from '../lib/dashboardCsv';
import { overdueFollowUpFilters } from '../lib/overdue';
import { PERIOD_LABEL } from '../lib/period';
import type { DashboardScopeFilters, DashboardSegmentFilters, ManagerActivityPeriod } from '../types';
import { dashboardKeys } from './dashboardKeys';
import { fetchDashboardStats } from './useDashboardStats';
import { fetchManagerActivity } from './useManagerActivity';

// The donor list endpoint's PageSize ceiling (GetDonorsQueryValidator).
const OVERDUE_EXPORT_LIMIT = 100;

interface ExportDashboardCsvVariables {
  scope: DashboardScopeFilters;
  segment: DashboardSegmentFilters;
  period: ManagerActivityPeriod;
  /** Human-readable labels of the active filters, written into the CSV header. */
  filterLabels: [string, string][];
  includePendingApprovals: boolean;
}

function todayYmd(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function downloadCsv(csv: string, fileName: string) {
  // BOM so Excel opens the file as UTF-8 (em dashes, accented names).
  const blob = new Blob(['﻿', csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/**
 * Downloads the dashboard as on screen (same filters + Summary period) as a CSV.
 * Built client-side from the same endpoints the widgets use, via fetchQuery on the
 * same query keys — so it reuses whatever is already cached instead of refetching,
 * and works for every role (the server-side report export is Admin-only).
 * A mutation, not a query: it has a side effect (a file download) and nothing to cache.
 */
export function useExportDashboardCsv() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, ExportDashboardCsvVariables>({
    mutationFn: async ({ scope, segment, period, filterLabels, includePendingApprovals }) => {
      const countFilters = { ...scope, ...segment, pageSize: 1 };
      const donorCount = async (filters: DonorFilters) => {
        const result = await queryClient.fetchQuery({
          queryKey: donorKeys.list(filters),
          queryFn: () => fetchDonors(filters),
        });
        return result.pagination.totalCount;
      };
      const overdueFilters = overdueFollowUpFilters(scope, OVERDUE_EXPORT_LIMIT);

      const [total, active, pendingReview, lapsed, stats, activity, overdue] = await Promise.all([
        donorCount(countFilters),
        donorCount({ ...countFilters, status: 'Active' }),
        donorCount({ ...countFilters, status: 'PendingReview' }),
        donorCount({ ...countFilters, status: 'Lapsed' }),
        queryClient.fetchQuery({ queryKey: dashboardKeys.stats(), queryFn: fetchDashboardStats }),
        queryClient.fetchQuery({
          queryKey: dashboardKeys.managerActivity(period),
          queryFn: () => fetchManagerActivity(period),
        }),
        queryClient.fetchQuery({
          queryKey: donorKeys.list(overdueFilters),
          queryFn: () => fetchDonors(overdueFilters),
        }),
      ]);

      const csv = buildDashboardCsv({
        generatedAt: new Date(),
        filters: filterLabels,
        donorCounts: { total, active, pendingReview, lapsed },
        stats,
        includePendingApprovals,
        periodLabel: PERIOD_LABEL[period],
        activity,
        overdue: overdue.data,
        overdueTotal: overdue.pagination.totalCount,
      });

      downloadCsv(csv, `sa-harvest-dashboard-${todayYmd()}.csv`);
    },
    onError: (error) => {
      toast.error(error.response?.data?.message ?? 'Export failed. Please try again.');
    },
  });
}
