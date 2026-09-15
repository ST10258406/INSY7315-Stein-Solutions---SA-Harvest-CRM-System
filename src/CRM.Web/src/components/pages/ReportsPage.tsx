import { useState } from 'react';
import {
  DonorsByRegionChart,
  DonorsByStatusChart,
  DonorsByTypeChart,
  DonorsContactedChart,
  ReportFilters,
} from '@/features/reports/components';
import { defaultDonorsContactedFilters } from '@/features/reports/hooks';
import type { DonorsContactedFilters } from '@/features/reports/types';

export default function ReportsPage() {
  // Owned here (not inside DonorsContactedChart) so ReportFilters and the chart it filters
  // share one source of truth — changing a filter re-renders only this section, leaving the
  // other three (always all-time, unfiltered) charts untouched.
  const [donorsContactedFilters, setDonorsContactedFilters] = useState<DonorsContactedFilters>(
    defaultDonorsContactedFilters,
  );

  return (
    <main className="flex-1 min-w-0 overflow-y-auto p-[26px_30px_34px]">
      <div className="mb-6">
        <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Reports</h1>
        <p className="m-0 mt-1.5 text-sm font-medium text-[var(--muted-c)]">
          Donor activity across the organisation.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start">
        <div>
          <ReportFilters value={donorsContactedFilters} onChange={setDonorsContactedFilters} />
          <DonorsContactedChart filters={donorsContactedFilters} />
        </div>
        <DonorsByStatusChart />
        <DonorsByRegionChart />
        <DonorsByTypeChart />
      </div>
    </main>
  );
}
