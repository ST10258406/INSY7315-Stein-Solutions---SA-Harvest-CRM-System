import { Download } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { RoleGuard } from '@/features/auth/components/RoleGuard';
import {
  useCompanyTypes,
  useDonationFrequencies,
  useDonationTypes,
  useOperationalRegions,
  useRelationshipManagers,
} from '@/features/lookups';
import { useExportReport } from '@/features/reports/hooks';
import { thisCalendarMonthRange } from '@/features/reports/lib/date';
import type { ExportFormat, ReportType } from '@/features/reports/types';
import { useAuthStore } from '@/store/authStore';
import { useExportDashboardCsv } from './hooks';
import type { DashboardScopeFilters, DashboardSegmentFilters, ManagerActivityPeriod } from './types';

const ADMIN_ROLES = ['Admin', 'SuperAdmin'];

const REPORTS: Array<{ reportType: ReportType; label: string }> = [
  { reportType: 'donors-contacted', label: 'Donors contacted (this month)' },
  { reportType: 'donors-by-status', label: 'Donors by status' },
  { reportType: 'donors-by-region', label: 'Donors by region' },
  { reportType: 'donors-by-type', label: 'Donors by donation type' },
];

interface DashboardExportMenuProps {
  scope: DashboardScopeFilters;
  segment: DashboardSegmentFilters;
  period: ManagerActivityPeriod;
}

/** Resolves the active dashboard filters to [label, display name] pairs for the CSV header. */
function useFilterLabels(scope: DashboardScopeFilters, segment: DashboardSegmentFilters): [string, string][] {
  const managers = useRelationshipManagers();
  const regions = useOperationalRegions();
  const companyTypes = useCompanyTypes();
  const donationTypes = useDonationTypes();
  const frequencies = useDonationFrequencies();

  const labels: [string, string][] = [];
  const add = (label: string, value: string | number | undefined, name: string | undefined) => {
    if (value !== undefined) labels.push([label, name ?? String(value)]);
  };
  add('Relationship manager', scope.relationshipManagerId, managers.data?.find((m) => m.id === scope.relationshipManagerId)?.fullName);
  add('Region', scope.regionCode, regions.data?.find((r) => r.code === scope.regionCode)?.name);
  add('Company type', segment.companyTypeId, companyTypes.data?.find((t) => t.id === segment.companyTypeId)?.name);
  add('Donation type', segment.donationTypeId, donationTypes.data?.find((t) => t.id === segment.donationTypeId)?.name);
  add('Donation frequency', segment.donationFrequencyId, frequencies.data?.find((f) => f.id === segment.donationFrequencyId)?.name);
  return labels;
}

/**
 * Dashboard "Export": a CSV snapshot of the dashboard as filtered, for every role,
 * plus — for Admins only, since POST /api/v1/reports/export is AdminOrAbove — the
 * same PDF/Excel report exports as the Reports page. The donors-contacted report
 * covers this calendar month and honours the dashboard's relationship-manager filter.
 */
export function DashboardExportMenu({ scope, segment, period }: DashboardExportMenuProps) {
  const filterLabels = useFilterLabels(scope, segment);
  const isAdmin = useAuthStore((s) => s.user?.roles.some((role) => ADMIN_ROLES.includes(role)) ?? false);
  const csvExport = useExportDashboardCsv();
  const reportExport = useExportReport();
  const isPending = csvExport.isPending || reportExport.isPending;

  function handleReportExport(reportType: ReportType, format: ExportFormat) {
    // Opened synchronously inside the click handler — see useExportReport's `target` doc.
    const target = window.open('', '_blank');
    reportExport.mutate({
      reportType,
      format,
      filters: { ...thisCalendarMonthRange(), relationshipManagerId: scope.relationshipManagerId },
      target,
    });
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button type="button" variant="secondary" size="sm" disabled={isPending}>
            <Download className="w-3.75 h-3.75" />
            <span>{isPending ? 'Exporting…' : 'Export'}</span>
          </Button>
        }
      />
      <DropdownMenuContent align="end" className="min-w-56">
        <DropdownMenuItem
          disabled={isPending}
          onClick={() =>
            csvExport.mutate({ scope, segment, period, filterLabels, includePendingApprovals: isAdmin })
          }
        >
          Dashboard snapshot (CSV)
        </DropdownMenuItem>

        <RoleGuard allowedRoles={ADMIN_ROLES}>
          <DropdownMenuSeparator />
          <DropdownMenuGroup>
            <DropdownMenuLabel>Reports</DropdownMenuLabel>
            {REPORTS.map(({ reportType, label }) => (
              <DropdownMenuSub key={reportType}>
                <DropdownMenuSubTrigger disabled={isPending}>{label}</DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <DropdownMenuItem disabled={isPending} onClick={() => handleReportExport(reportType, 'pdf')}>
                    Export as PDF
                  </DropdownMenuItem>
                  <DropdownMenuItem disabled={isPending} onClick={() => handleReportExport(reportType, 'excel')}>
                    Export as Excel
                  </DropdownMenuItem>
                </DropdownMenuSubContent>
              </DropdownMenuSub>
            ))}
          </DropdownMenuGroup>
        </RoleGuard>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
