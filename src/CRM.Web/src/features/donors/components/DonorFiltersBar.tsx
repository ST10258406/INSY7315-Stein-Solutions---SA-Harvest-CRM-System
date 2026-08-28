import { useEffect, useState } from 'react';
import { Search, SlidersHorizontal, X } from 'lucide-react';
import { useCompanyTypes, useOperationalRegions, useDonationTypes, useDonationFrequencies } from '@/features/lookups';
import type { DonorFilters, DonorStatus } from '../types';
import type { DonorFilterKey } from '../hooks/useDonorListFilters';

const STATUS_OPTIONS: { value: DonorStatus; label: string }[] = [
  { value: 'PendingReview', label: 'Pending review' },
  { value: 'Active', label: 'Active' },
  { value: 'Lapsed', label: 'Lapsed' },
  { value: 'Rejected', label: 'Rejected' },
];

const SELECT_CLASSES =
  'h-9 rounded-lg border border-[#2B2B23] bg-[#141410] px-3 text-sm text-[#F4F4EE] outline-none focus-visible:border-[#F4F4EE] disabled:opacity-50';

interface DonorFiltersBarProps {
  filters: DonorFilters;
  onFilterChange: (key: DonorFilterKey, value: string | undefined) => void;
  onClear: () => void;
}

export function DonorFiltersBar({ filters, onFilterChange, onClear }: DonorFiltersBarProps) {
  const [showMore, setShowMore] = useState(false);
  const [searchInput, setSearchInput] = useState(filters.search ?? '');
  const [syncedSearch, setSyncedSearch] = useState(filters.search);

  const companyTypes = useCompanyTypes();
  const regions = useOperationalRegions();
  const donationTypes = useDonationTypes();
  const donationFrequencies = useDonationFrequencies();

  // Keep the input in sync when a filter reset (e.g. "Clear filters") changes
  // `filters.search` out from under the local draft. Adjusted during render
  // (React's sanctioned pattern for "state derived from a changed prop")
  // rather than an effect, since an effect would set state after an extra
  // render/paint instead of before this one commits.
  if (filters.search !== syncedSearch) {
    setSyncedSearch(filters.search);
    setSearchInput(filters.search ?? '');
  }

  // Debounce the search box: only push to the URL (and therefore refetch)
  // 400ms after the user stops typing, rather than on every keystroke.
  useEffect(() => {
    const trimmed = searchInput.trim();
    if (trimmed === (filters.search ?? '')) return;

    const timeout = setTimeout(() => {
      onFilterChange('search', trimmed || undefined);
    }, 400);

    return () => clearTimeout(timeout);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchInput]);

  const hasActiveFilters =
    !!filters.search ||
    !!filters.status ||
    !!filters.companyTypeId ||
    !!filters.regionCode ||
    !!filters.donationTypeId ||
    !!filters.donationFrequencyId ||
    !!filters.followUpBefore;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2.5">
        <div className="relative min-w-[220px] flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-[#6B6B60]" />
          <input
            type="text"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search donors by company name…"
            className="h-9 w-full rounded-lg border border-[#2B2B23] bg-[#141410] pr-3 pl-9 text-sm text-[#F4F4EE] placeholder:text-[#6B6B60] outline-none focus-visible:border-[#F4F4EE]"
          />
        </div>

        <select
          value={filters.status ?? ''}
          onChange={(e) => onFilterChange('status', e.target.value || undefined)}
          className={SELECT_CLASSES}
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>

        <button
          type="button"
          onClick={() => setShowMore((v) => !v)}
          aria-expanded={showMore}
          className={`flex h-9 items-center gap-1.5 rounded-lg border px-3 text-sm font-medium transition-colors ${
            showMore
              ? 'border-brand bg-brand/10 text-brand'
              : 'border-[#2B2B23] bg-[#141410] text-[#B9B9AE] hover:border-[#F4F4EE]'
          }`}
        >
          <SlidersHorizontal className="h-3.5 w-3.5" />
          More filters
        </button>

        {hasActiveFilters && (
          <button
            type="button"
            onClick={onClear}
            className="flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-[#B9B9AE] transition-colors hover:text-[#F4F4EE]"
          >
            <X className="h-3.5 w-3.5" />
            Clear filters
          </button>
        )}
      </div>

      {showMore && (
        <div className="flex flex-wrap items-center gap-2.5 rounded-xl border border-[#2B2B23] bg-[#141410]/60 p-3">
          <select
            value={filters.companyTypeId ?? ''}
            onChange={(e) => onFilterChange('companyTypeId', e.target.value || undefined)}
            disabled={companyTypes.isPending}
            className={SELECT_CLASSES}
          >
            <option value="">All company types</option>
            {companyTypes.data?.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>

          <select
            value={filters.regionCode ?? ''}
            onChange={(e) => onFilterChange('regionCode', e.target.value || undefined)}
            disabled={regions.isPending}
            className={SELECT_CLASSES}
          >
            <option value="">All regions</option>
            {regions.data?.map((region) => (
              <option key={region.id} value={region.code}>
                {region.name}
              </option>
            ))}
          </select>

          <select
            value={filters.donationTypeId ?? ''}
            onChange={(e) => onFilterChange('donationTypeId', e.target.value || undefined)}
            disabled={donationTypes.isPending}
            className={SELECT_CLASSES}
          >
            <option value="">All donation types</option>
            {donationTypes.data?.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>

          <select
            value={filters.donationFrequencyId ?? ''}
            onChange={(e) => onFilterChange('donationFrequencyId', e.target.value || undefined)}
            disabled={donationFrequencies.isPending}
            className={SELECT_CLASSES}
          >
            <option value="">All frequencies</option>
            {donationFrequencies.data?.map((frequency) => (
              <option key={frequency.id} value={frequency.id}>
                {frequency.name}
              </option>
            ))}
          </select>

          <label className="flex items-center gap-2 text-sm text-[#B9B9AE]">
            Follow-up before
            <input
              type="date"
              value={filters.followUpBefore ?? ''}
              onChange={(e) => onFilterChange('followUpBefore', e.target.value || undefined)}
              className={SELECT_CLASSES}
            />
          </label>
        </div>
      )}
    </div>
  );
}
