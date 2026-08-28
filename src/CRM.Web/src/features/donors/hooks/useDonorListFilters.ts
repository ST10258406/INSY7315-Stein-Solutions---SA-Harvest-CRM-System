import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { DonorFilters, DonorSortField, DonorStatus } from '../types';

export const DEFAULT_PAGE_SIZE = 20;

const SORT_FIELDS: DonorSortField[] = ['followUpDate', 'companyName', 'createdAt', 'lastInteractionDate'];
const STATUSES: DonorStatus[] = ['PendingReview', 'Active', 'Lapsed', 'Rejected'];

export type DonorFilterKey =
  | 'search'
  | 'status'
  | 'companyTypeId'
  | 'regionCode'
  | 'donationTypeId'
  | 'donationFrequencyId'
  | 'followUpBefore';

function toInt(value: string | null): number | undefined {
  if (!value) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

/**
 * Keeps donor list search/filter/sort/pagination state in the URL (query
 * string) rather than component state, so the list is bookmarkable/shareable
 * and survives a back-navigation from the detail page. All updaters read
 * `prev` from the setSearchParams callback rather than the `filters` closure,
 * so rapid successive calls (e.g. clearing two filters back-to-back) never
 * clobber each other with a stale snapshot.
 */
export function useDonorListFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters: DonorFilters = useMemo(() => {
    const status = searchParams.get('status');
    const sortBy = searchParams.get('sortBy');
    const sortDir = searchParams.get('sortDir');

    return {
      page: toInt(searchParams.get('page')) ?? 1,
      pageSize: toInt(searchParams.get('pageSize')) ?? DEFAULT_PAGE_SIZE,
      search: searchParams.get('search') ?? undefined,
      status: status && STATUSES.includes(status as DonorStatus) ? (status as DonorStatus) : undefined,
      companyTypeId: toInt(searchParams.get('companyTypeId')),
      regionCode: searchParams.get('regionCode') ?? undefined,
      donationTypeId: toInt(searchParams.get('donationTypeId')),
      donationFrequencyId: toInt(searchParams.get('donationFrequencyId')),
      followUpBefore: searchParams.get('followUpBefore') ?? undefined,
      sortBy: sortBy && SORT_FIELDS.includes(sortBy as DonorSortField) ? (sortBy as DonorSortField) : undefined,
      sortDir: sortDir === 'desc' ? 'desc' : sortDir === 'asc' ? 'asc' : undefined,
    };
  }, [searchParams]);

  const setFilter = useCallback(
    (key: DonorFilterKey, value: string | undefined) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (value) {
          next.set(key, value);
        } else {
          next.delete(key);
        }
        next.delete('page'); // any filter change starts back at page 1
        return next;
      });
    },
    [setSearchParams]
  );

  const setSort = useCallback(
    (field: DonorSortField) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        const isSameField = prev.get('sortBy') === field;
        const currentDir = prev.get('sortDir') ?? 'asc';
        next.set('sortBy', field);
        next.set('sortDir', isSameField && currentDir === 'asc' ? 'desc' : 'asc');
        next.delete('page');
        return next;
      });
    },
    [setSearchParams]
  );

  const setPage = useCallback(
    (page: number) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('page', String(page));
        return next;
      });
    },
    [setSearchParams]
  );

  const setPageSize = useCallback(
    (pageSize: number) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('pageSize', String(pageSize));
        next.delete('page');
        return next;
      });
    },
    [setSearchParams]
  );

  const clearFilters = useCallback(() => {
    setSearchParams({});
  }, [setSearchParams]);

  return { filters, setFilter, setSort, setPage, setPageSize, clearFilters };
}
