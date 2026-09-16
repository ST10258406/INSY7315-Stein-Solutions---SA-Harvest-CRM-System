import { useState } from 'react';
import {
  DonorsByRegionChart,
  DonorsByStatusChart,
  DonorsByTypeChart,
  DonorsContactedChart,
  ReportFilters,
  ReportsExportMenu,
} from '@/features/reports/components';
import { defaultDonorsContactedFilters } from '@/features/reports/hooks';
import type { DonorsContactedFilters } from '@/features/reports/types';

export default function ReportsPage() {
  // Owned here (not inside DonorsContactedChart) so ReportFilters and the chart it filters
  // share one source of truth — changing a filter re-renders only this section, leaving the
  // other three (always all-time, unfiltered) charts untouched. ReportsExportMenu also needs it,
  // since the donors-contacted export must respect the same date range/manager as the chart.
  const [donorsContactedFilters, setDonorsContactedFilters] = useState<DonorsContactedFilters>(
    defaultDonorsContactedFilters,
  );

  return (
    <main className="flex-1 min-w-0 overflow-y-auto p-[26px_30px_34px]">
      {/* Mirrors DashboardPage's header banner: title/subtitle on the left, page-level actions
          pinned to the right via ml-auto. */}
      <div className="flex items-end gap-6 flex-wrap mb-6">
        <div>
          <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Reports</h1>
          <p className="m-0 mt-1.5 text-sm font-medium text-[var(--muted-c)]">
            Donor activity across the organisation.
          </p>
        </div>
        <div className="ml-auto">
          <ReportsExportMenu donorsContactedFilters={donorsContactedFilters} />
        </div>
      </div>

      {/* Full-width, above the grid — not inside DonorsContactedChart's own grid cell, which
          would make that cell taller than its row-sibling and throw off the 2x2 alignment. */}
      <ReportFilters value={donorsContactedFilters} onChange={setDonorsContactedFilters} />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start">
        <DonorsContactedChart filters={donorsContactedFilters} />
        <DonorsByStatusChart />
        <DonorsByRegionChart />
        <DonorsByTypeChart />
      </div>
    </main>
  );
}
