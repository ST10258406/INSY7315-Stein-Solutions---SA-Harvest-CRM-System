import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import type { ApiError, ExportFormat, ExportReportFilters, ReportExportResult, ReportType } from '../types';

interface ExportReportVariables {
  reportType: ReportType;
  format: ExportFormat;
  /** Ignored server-side for every report type except donors-contacted. */
  filters?: ExportReportFilters;
}

/**
 * Fetches a fresh 15-minute SAS URL for the requested report export and triggers the browser
 * download immediately — mirrors useDownloadDonorDocument. The URL is deliberately never
 * returned or cached: fire-and-forget only.
 */
export function useExportReport() {
  return useMutation<void, ApiError, ExportReportVariables>({
    mutationFn: async ({ reportType, format, filters }) => {
      const { data } = await api.post<{ data: ReportExportResult }>('/api/v1/reports/export', {
        reportType,
        format,
        filters: reportType === 'donors-contacted' ? filters : undefined,
      });

      const link = document.createElement('a');
      link.href = data.data.downloadUrl;
      link.download = data.data.fileName;
      link.rel = 'noopener';
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      link.remove();
    },
  });
}
