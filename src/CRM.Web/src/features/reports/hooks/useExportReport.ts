import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/axios';
import type { ApiError, ExportFormat, ExportReportFilters, ReportExportResult, ReportType } from '../types';

interface ExportReportVariables {
  reportType: ReportType;
  format: ExportFormat;
  /** Ignored server-side for every report type except donors-contacted. */
  filters?: ExportReportFilters;
  /**
   * A tab opened with `window.open('', '_blank')` synchronously inside the click handler that
   * triggered this mutation — before `mutate` was called, not after. Safari (unlike Chrome)
   * drops "user activation" the instant control yields to an awaited request, so a
   * window.open() called from this mutation's own onSuccess is reliably blocked there; this
   * mutation only ever redirects a tab that already exists. `null` means even that synchronous
   * open was blocked — rare, but onSuccess/onError below still handle it instead of throwing.
   */
  target: Window | null;
}

/**
 * Fires POST /api/v1/reports/export and, on success, redirects the caller's pre-opened tab to
 * the returned SAS URL. A useMutation, not a useQuery: there's no queryKey, so nothing is
 * cached and every call to `mutate` is a fresh network request — a second click never reuses a
 * previous (possibly already-expired) downloadUrl.
 *
 * The page-level ReportsExportMenu uses this one hook for every report, so the in-flight
 * (`isPending`) state and the error toast both live here once rather than being re-implemented
 * per menu item. Opening the tab itself can't live here too — see `target` above — but
 * redirecting or closing it does.
 */
export function useExportReport() {
  return useMutation<ReportExportResult, ApiError, ExportReportVariables>({
    mutationFn: async ({ reportType, format, filters }) => {
      const { data } = await api.post<{ data: ReportExportResult }>('/api/v1/reports/export', {
        reportType,
        format,
        filters: reportType === 'donors-contacted' ? filters : undefined,
      });
      return data.data;
    },
    onSuccess: (result, variables) => {
      if (variables.target) {
        variables.target.location.href = result.downloadUrl;
      } else {
        toast.error('Your browser blocked the export tab. Please allow pop-ups for this site and try again.');
      }
    },
    onError: (error, variables) => {
      variables.target?.close();
      toast.error(error.response?.data?.message ?? 'Export failed. Please try again.');
    },
  });
}
