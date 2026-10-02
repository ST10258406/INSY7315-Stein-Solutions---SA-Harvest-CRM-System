import { useEffect, useState } from 'react';
import { Search, ChevronDown } from 'lucide-react';
import { useRoles } from '../hooks';
import type { UserFilters } from '../types';
import { Button } from '@/components/ui/button';

interface UserFiltersBarProps {
  filters: UserFilters;
  resultCount?: number;
  onSearch: (value: string) => void;
  onStatusChange: (status: 'all' | 'active' | 'deactivated') => void;
  onRoleChange: (roleId: string | undefined) => void;
}

const STATUS_TABS: { value: 'all' | 'active' | 'deactivated'; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'active', label: 'Active' },
  { value: 'deactivated', label: 'Deactivated' },
];

export function UserFiltersBar({ filters, resultCount, onSearch, onStatusChange, onRoleChange }: UserFiltersBarProps) {
  const roles = useRoles();
  const [searchInput, setSearchInput] = useState(filters.search ?? '');
  const [syncedSearch, setSyncedSearch] = useState(filters.search);

  if (filters.search !== syncedSearch) {
    setSyncedSearch(filters.search);
    setSearchInput(filters.search ?? '');
  }

  useEffect(() => {
    const trimmed = searchInput.trim();
    if (trimmed === (filters.search ?? '')) return;

    const timeout = setTimeout(() => onSearch(trimmed), 400);
    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchInput]);

  const currentStatus: 'all' | 'active' | 'deactivated' =
    filters.isActive === true ? 'active' : filters.isActive === false ? 'deactivated' : 'all';

  return (
    <div className="mb-5 flex flex-wrap items-center gap-2.5">
      <div className="relative min-w-[240px] max-w-[360px] flex-1">
        <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-4">
          <Search className="h-4 w-4 text-[var(--icon)]" />
        </div>
        <input
          type="text"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Search by name or email…"
          className="h-[42px] w-full rounded-full border border-[var(--border)] bg-[var(--card)] pr-4 pl-10 text-[13.5px] font-medium text-[var(--ink)] placeholder-[var(--muted-c)] shadow-sm outline-none transition-colors focus:border-[var(--ink)]"
        />
      </div>

      <div className="flex shrink-0 gap-1 rounded-full border border-[var(--border)] bg-[var(--card)] p-1">
        {STATUS_TABS.map((tab) => (
          <Button
            key={tab.value}
            type="button"
            size="sm"
            variant={currentStatus === tab.value ? 'dark' : 'ghost'}
            onClick={() => onStatusChange(tab.value)}
            className="h-8.5 rounded-full px-4 text-[12.5px]"
          >
            {tab.label}
          </Button>
        ))}
      </div>

      <div className="relative shrink-0">
        <select
          value={filters.roleId ?? ''}
          onChange={(e) => onRoleChange(e.target.value || undefined)}
          disabled={roles.isPending}
          className="h-[42px] appearance-none rounded-full border border-[var(--border)] bg-[var(--card)] pr-8 pl-3.5 text-[12.5px] font-semibold text-[var(--ink)] outline-none transition-colors focus-visible:border-[var(--ink)] disabled:opacity-50"
        >
          <option value="">Role: All</option>
          {roles.data?.map((role) => (
            <option key={role.id} value={role.id}>
              {role.name}
            </option>
          ))}
        </select>
        <ChevronDown className="pointer-events-none absolute top-1/2 right-3 h-3.5 w-3.5 -translate-y-1/2 text-[var(--icon)]" />
      </div>

      {resultCount !== undefined && (
        <span className="ml-auto text-xs font-semibold text-[var(--muted-c)]">
          {resultCount} user{resultCount === 1 ? '' : 's'}
        </span>
      )}
    </div>
  );
}
