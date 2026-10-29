import { TriangleAlert } from 'lucide-react';
import type { ApiError } from '@/features/donors/types';

/** Loading / error / empty row for the plain vetting tables. Returns null when there are rows to show. */
export function VettingTableStates({
  colSpan,
  isPending,
  isError,
  error,
  onRetry,
  isEmpty,
  emptyText,
}: {
  colSpan: number;
  isPending: boolean;
  isError: boolean;
  error?: ApiError | null;
  onRetry: () => void;
  isEmpty: boolean;
  emptyText: string;
}) {
  if (!isPending && !isError && !isEmpty) return null;
  return (
    <tr>
      <td colSpan={colSpan} className="px-4 py-14 text-center text-sm">
        {isPending ? (
          <span className="text-muted-foreground">Loading…</span>
        ) : isError ? (
          <span className="flex flex-col items-center gap-2">
            <TriangleAlert className="h-5 w-5 text-muted-foreground" />
            <span className="font-semibold text-foreground">{error?.response?.data?.message ?? 'Something went wrong.'}</span>
            <button type="button" onClick={onRetry} className="rounded-full border border-border px-3 py-1 text-xs font-medium hover:border-foreground/60">
              Retry
            </button>
          </span>
        ) : (
          <span className="font-semibold text-muted-foreground">{emptyText}</span>
        )}
      </td>
    </tr>
  );
}

export const TH = 'px-4 py-3.75 text-left text-[11px] font-bold tracking-wide text-[var(--muted-c)] uppercase';
export const TD = 'px-4 py-3.5 text-[13px] font-medium text-foreground border-t border-[var(--hair)]';
