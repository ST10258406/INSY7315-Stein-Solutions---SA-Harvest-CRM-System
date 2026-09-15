import { DownloadSimple } from '@phosphor-icons/react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useExportReport } from '../hooks/useExportReport';
import type { ExportReportFilters, ReportType } from '../types';

interface ExportReportButtonProps {
  reportType: ReportType;
  /** Only meaningful (and only sent) for the donors-contacted report. */
  filters?: ExportReportFilters;
}

/**
 * PDF/Excel export for one report section, per the typed `reportType` export design — there is
 * no single global export action. Disabled while a request is in flight so a double-click can't
 * fire two export jobs for the same report.
 */
export function ExportReportButton({ reportType, filters }: ExportReportButtonProps) {
  const { mutate, isPending } = useExportReport();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          disabled={isPending}
          aria-label="Export report"
        >
          <DownloadSimple className="h-3.5 w-3.5" />
          {isPending ? 'Exporting…' : 'Export'}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem disabled={isPending} onClick={() => mutate({ reportType, format: 'pdf', filters })}>
          Export as PDF
        </DropdownMenuItem>
        <DropdownMenuItem disabled={isPending} onClick={() => mutate({ reportType, format: 'excel', filters })}>
          Export as Excel
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
