import { DownloadSimple } from '@phosphor-icons/react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useExportReport } from '../hooks/useExportReport';
import type { DonorsContactedFilters, ExportFormat, ReportType } from '../types';

interface ReportsExportMenuProps {
  /** Only sent for the donors-contacted export — every other report ignores filters entirely. */
  donorsContactedFilters: DonorsContactedFilters;
}

const REPORTS: Array<{ reportType: ReportType; label: string }> = [
  { reportType: 'donors-contacted', label: 'Donors contacted' },
  { reportType: 'donors-by-status', label: 'Donors by status' },
  { reportType: 'donors-by-region', label: 'Donors by region' },
  { reportType: 'donors-by-type', label: 'Donors by donation type' },
];

/**
 * One page-level Export control (top right of the Reports page, next to the page heading) that
 * covers all four reports, per the typed `reportType` export design — a submenu per report,
 * each with PDF/Excel, rather than four separate per-card export buttons.
 */
export function ReportsExportMenu({ donorsContactedFilters }: ReportsExportMenuProps) {
  const { mutate, isPending } = useExportReport();

  function handleExport(reportType: ReportType, format: ExportFormat) {
    // Opened here, synchronously inside the click handler — see useExportReport's `target` doc
    // for why this can't happen after the export request resolves (Safari blocks that).
    const target = window.open('', '_blank');
    mutate({
      reportType,
      format,
      filters: reportType === 'donors-contacted' ? donorsContactedFilters : undefined,
      target,
    });
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button type="button" variant="secondary" size="sm" disabled={isPending}>
            <DownloadSimple className="h-3.75 w-3.75" />
            <span>{isPending ? 'Exporting…' : 'Export'}</span>
          </Button>
        }
      />
      <DropdownMenuContent align="end">
        {REPORTS.map(({ reportType, label }) => (
          <DropdownMenuSub key={reportType}>
            <DropdownMenuSubTrigger disabled={isPending}>{label}</DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              <DropdownMenuItem disabled={isPending} onClick={() => handleExport(reportType, 'pdf')}>
                Export as PDF
              </DropdownMenuItem>
              <DropdownMenuItem disabled={isPending} onClick={() => handleExport(reportType, 'excel')}>
                Export as Excel
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuSub>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
