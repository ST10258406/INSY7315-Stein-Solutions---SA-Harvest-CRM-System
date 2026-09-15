import type { DonorStatus } from '@/features/donors/types';

// Mirrors the backend DTOs in CRM.Application/Modules/Reports/Dtos exactly —
// keep these in sync with that project. Every endpoint is wrapped in the
// standard `{ data: ... }` envelope by ReportsController, which the hooks
// below unwrap.

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
