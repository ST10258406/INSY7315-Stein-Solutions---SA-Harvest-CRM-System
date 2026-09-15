import type { AxiosError } from 'axios';
import type { ApiErrorEnvelope } from '@/lib/apiError';
import type { DonorStatus } from '@/features/donors/types';

// Mirrors the backend DTOs in CRM.Application/Modules/Reports/Dtos exactly —
// keep these in sync with that project. Every endpoint is wrapped in the
// standard `{ data: ... }` envelope by ReportsController, which the hooks
// below unwrap.

/** Error shape thrown by every report hook — matches the backend's standard envelope. */
export type ApiError = AxiosError<ApiErrorEnvelope>;

export interface ReportPeriod {
  startDate: string; // yyyy-MM-dd (DateOnly)
  endDate: string;
}

export interface ReportManager {
  id: string;
  fullName: string;
}

export interface ManagerContacted {
  manager: ReportManager;
  /** Distinct donors this manager had at least one interaction with in the period. */
  donorsContacted: number;
  /** Total interaction rows for this manager's donors in the period (not distinct). */
  totalInteractions: number;
}

export interface DonorsContactedReport {
  period: ReportPeriod;
  /**
   * Distinct donor count across the whole period. Computed independently of
   * byManager on the backend — never a sum of its per-manager values.
   */
  totalDonorsContacted: number;
  byManager: ManagerContacted[];
}

/** GetDonorsContactedReportQuery — StartDate/EndDate are required by the backend validator. */
export interface DonorsContactedFilters {
  startDate: string; // yyyy-MM-dd
  endDate: string;
  relationshipManagerId?: string;
}

export interface DonorsByRegion {
  region: string; // lookup code
  regionName: string; // display name
  donorCount: number;
}

export interface DonorsByType {
  donationType: string;
  donorCount: number;
}

export interface DonorsByStatus {
  status: DonorStatus;
  donorCount: number;
}

// Mirrors CRM.Application.Modules.Reports.Export.ReportTypes / ReportExportFormats and
// POST /api/v1/reports/export's request/response DTOs.

export type ReportType = 'donors-contacted' | 'donors-by-region' | 'donors-by-type' | 'donors-by-status';

export type ExportFormat = 'pdf' | 'excel';

/** Only meaningful for the donors-contacted export — every other report type ignores it. */
export type ExportReportFilters = DonorsContactedFilters;

export interface ReportExportResult {
  downloadUrl: string;
  expiresAt: string;
  fileName: string;
  fileSizeBytes: number;
}
