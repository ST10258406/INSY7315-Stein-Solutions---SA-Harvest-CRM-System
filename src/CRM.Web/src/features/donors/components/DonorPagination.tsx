import { ChevronLeft, ChevronRight, ChevronDown } from 'lucide-react';
import type { PaginationMeta } from '../types';
import { Button } from '@/components/ui/button';

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];
const MAX_PAGE_BUTTONS = 7;

interface DonorPaginationProps {
  pagination: PaginationMeta;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export function DonorPagination({ pagination, onPageChange, onPageSizeChange }: DonorPaginationProps) {
  const { page, pageSize, totalCount, totalPages } = pagination;
  const rangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const rangeEnd = Math.min(page * pageSize, totalCount);
  const showNumberedPages = totalPages > 0 && totalPages <= MAX_PAGE_BUTTONS;

  return (
    <div className="flex flex-wrap items-center justify-end gap-4.5 mt-4 pt-4 border-t border-[var(--border)]">
      <span className="text-[12.5px] font-medium text-[var(--muted-c)]">
        {totalCount ? `Showing ${rangeStart}–${rangeEnd} of ${totalCount} donors` : 'Showing 0 of 0 donors'}
      </span>

      <div className="flex items-center gap-2">
        <span className="text-[12.5px] font-medium text-[var(--muted-c)]">Rows</span>
        <div className="relative">
          <select
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className="h-8.5 appearance-none rounded-full border border-[var(--border)] bg-[var(--card)] pr-7 pl-3 text-[12.5px] font-bold text-[var(--ink)] outline-none focus:border-[var(--ink)] transition-colors cursor-pointer"
          >
            {PAGE_SIZE_OPTIONS.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
          <ChevronDown className="pointer-events-none absolute top-1/2 right-2.5 h-3 w-3 -translate-y-1/2 text-[var(--icon)]" />
        </div>
      </div>

      <div className="flex items-center gap-1.5">
        <Button
          variant="secondary"
          size="icon"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Previous page"
          className="h-8.5 w-8.5 text-[var(--icon)] hover:text-[var(--ink)]"
        >
          <ChevronLeft className="h-3.5 w-3.5" />
        </Button>

        {showNumberedPages ? (
          Array.from({ length: totalPages }, (_, i) => i + 1).map((n) => (
            <Button
              key={n}
              variant={n === page ? 'default' : 'secondary'}
              onClick={() => onPageChange(n)}
              className={`h-8.5 min-w-[34px] px-2.5 text-[12.5px] ${
                n === page ? 'bg-[var(--brand)] text-[var(--primary-foreground)]' : ''
              }`}
            >
              {n}
            </Button>
          ))
        ) : (
          <span className="px-1 text-[12.5px] font-semibold text-[var(--ink)]">
            Page {totalPages === 0 ? 0 : page} of {totalPages}
          </span>
        )}

        <Button
          variant="secondary"
          size="icon"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Next page"
          className="h-8.5 w-8.5 text-[var(--icon)] hover:text-[var(--ink)]"
        >
          <ChevronRight className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  );
}
