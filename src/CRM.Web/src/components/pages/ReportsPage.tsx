import {
  DonorsByRegionChart,
  DonorsByStatusChart,
  DonorsByTypeChart,
  DonorsContactedChart,
} from '@/features/reports/components';

// TODO(next issue — ReportFilters): render the date-range control above the
// grid and pass its value as `filters` to DonorsContactedChart. The other
// three charts are always all-time, per the design doc — don't wire a date
// picker into them.
export default function ReportsPage() {
  return (
    <main className="flex-1 min-w-0 overflow-y-auto p-[26px_30px_34px]">
      <div className="mb-6">
        <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Reports</h1>
        <p className="m-0 mt-1.5 text-sm font-medium text-[var(--muted-c)]">
          Donor activity across the organisation.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start">
        <DonorsContactedChart />
        <DonorsByStatusChart />
        <DonorsByRegionChart />
        <DonorsByTypeChart />
      </div>
    </main>
  );
}
