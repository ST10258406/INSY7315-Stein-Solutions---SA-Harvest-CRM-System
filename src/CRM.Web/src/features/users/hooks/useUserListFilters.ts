import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { UserFilters, UserSortField } from '../types';

export const DEFAULT_PAGE_SIZE = 20;

const SORT_FIELDS: UserSortField[] = ['name', 'email', 'createdAt'];

function toInt(value: string | null): number | undefined {
  if (!value) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

/**
 * Keeps the user list's search/status/role/sort/pagination state in the URL,
 * mirroring useDonorListFilters — bookmarkable/shareable and consistent with
 * the rest of the app rather than plain component state.
 */
export function useUserListFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters: UserFilters = useMemo(() => {
    const status = searchParams.get('status'); // 'active' | 'deactivated' | null (All)
    const sortBy = searchParams.get('sortBy');
    const sortDir = searchParams.get('sortDir');

    return {
      page: toInt(searchParams.get('page')) ?? 1,
      pageSize: toInt(searchParams.get('pageSize')) ?? DEFAULT_PAGE_SIZE,
      search: searchParams.get('search') ?? undefined,
      isActive: status === 'active' ? true : status === 'deactivated' ? false : undefined,
      roleId: searchParams.get('roleId') ?? undefined,
      sortBy: sortBy && SORT_FIELDS.includes(sortBy as UserSortField) ? (sortBy as UserSortField) : undefined,
      sortDir: sortDir === 'desc' ? 'desc' : sortDir === 'asc' ? 'asc' : undefined,
    };
  }, [searchParams]);

  const setSearch = useCallback(
    (value: string) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (value) next.set('search', value);
        else next.delete('search');
        next.delete('page');
        return next;
      });
    },
    [setSearchParams]
  );

  const setStatus = useCallback(
    (status: 'all' | 'active' | 'deactivated') => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (status === 'all') next.delete('status');
        else next.set('status', status);
        next.delete('page');
        return next;
      });
    },
    [setSearchParams]
  );

  const setRoleId = useCallback(
    (roleId: string | undefined) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (roleId) next.set('roleId', roleId);
        else next.delete('roleId');
        next.delete('page');
        return next;
      });
    },
    [setSearchParams]
  );

  const setSort = useCallback(
    (field: UserSortField) => {
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

  return { filters, setSearch, setStatus, setRoleId, setSort, setPage, setPageSize };
}
