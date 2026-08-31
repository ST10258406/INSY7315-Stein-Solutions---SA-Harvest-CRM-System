import { useEffect, useState } from 'react';
import type { SelectHTMLAttributes } from 'react';
import { Search, SlidersHorizontal, ChevronDown, FilterX } from 'lucide-react';
import { useCompanyTypes, useOperationalRegions, useDonationTypes, useDonationFrequencies } from '@/features/lookups';
import type { DonorFilters, DonorStatus } from '../types';
import type { DonorFilterKey } from '../hooks/useDonorListFilters';
import { Button } from '@/components/ui/button';

const STATUS_OPTIONS: { value: DonorStatus; label: string }[] = [
  { value: 'PendingReview', label: 'Pending review' },
  { value: 'Active', label: 'Active' },
  { value: 'Lapsed', label: 'Lapsed' },
  { value: 'Rejected', label: 'Rejected' },
];

const PILL_SELECT_CLASSES =
  'h-[42px] appearance-none rounded-full border border-[var(--border)] bg-[var(--card)] pr-8 pl-3.5 text-[12.5px] font-semibold text-[var(--ink)] outline-none focus-visible:border-[var(--ink)] disabled:opacity-50 transition-colors';

const PILL_SELECT_ACTIVE_CLASSES =
  'h-[42px] appearance-none rounded-full border border-brand bg-brand pr-8 pl-3.5 text-[12.5px] font-semibold text-primary-foreground outline-none transition-colors';

interface DonorFiltersBarProps {
  filters: DonorFilters;
  onFilterChange: (key: DonorFilterKey, value: string | undefined) => void;
  onClear: () => void;
}

function PillSelect({
  className,
  isActive,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & { isActive?: boolean }) {
  return (
    <div className="relative shrink-0">
      <select 
        {...props} 
        className={`${isActive ? PILL_SELECT_ACTIVE_CLASSES : PILL_SELECT_CLASSES} ${className ?? ''}`} 
      />
      <ChevronDown className={`pointer-events-none absolute top-1/2 right-3 h-3.5 w-3.5 -translate-y-1/2 ${isActive ? 'text-primary-foreground' : 'text-[var(--icon)]'}`} />
    </div>
  );
}

export function DonorFiltersBar({ filters, onFilterChange, onClear }: DonorFiltersBarProps) {
  const [showMore, setShowMore] = useState(false);
  const [searchInput, setSearchInput] = useState(filters.search ?? '');
  const [syncedSearch, setSyncedSearch] = useState(filters.search);

  const companyTypes = useCompanyTypes();
  const regions = useOperationalRegions();
  const donationTypes = useDonationTypes();
  const donationFrequencies = useDonationFrequencies();

  if (filters.search !== syncedSearch) {
    setSyncedSearch(filters.search);
    setSearchInput(filters.search ?? '');
  }

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
    <div className="mb-5">
      <div className="flex flex-wrap items-center gap-2.5">
        <div className="relative flex-1 min-w-[280px]">
          <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
            <Search className="h-4 w-4 text-[var(--icon)]" />
          </div>
          <input
            type="text"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search donors by company name…"
            className="w-full h-[42px] pl-10 pr-4 rounded-full border border-[var(--border)] bg-[var(--card)] text-[13.5px] font-medium text-[var(--ink)] placeholder-[var(--muted-c)] focus:outline-none focus:border-[var(--ink)] transition-colors shadow-sm"
          />
        </div>

        <PillSelect 
          value={filters.status ?? ''} 
          onChange={(e) => onFilterChange('status', e.target.value || undefined)}
          isActive={!!filters.status}
        >
          <option value="">Status: All</option>
          {STATUS_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </PillSelect>
        
        <PillSelect
          value={filters.companyTypeId ?? ''}
          onChange={(e) => onFilterChange('companyTypeId', e.target.value || undefined)}
          disabled={companyTypes.isPending}
          isActive={!!filters.companyTypeId}
        >
          <option value="">Type: All</option>
          {companyTypes.data?.map((type) => (
            <option key={type.id} value={type.id}>
              {type.name}
            </option>
          ))}
        </PillSelect>

        <PillSelect
          value={filters.regionCode ?? ''}
          onChange={(e) => onFilterChange('regionCode', e.target.value || undefined)}
          disabled={regions.isPending}
          isActive={!!filters.regionCode}
        >
          <option value="">Region: All</option>
          {regions.data?.map((region) => (
            <option key={region.id} value={region.code}>
              {region.name}
            </option>
          ))}
        </PillSelect>

        <Button
          variant={showMore ? 'dark' : 'secondary'}
          onClick={() => setShowMore((v) => !v)}
          aria-expanded={showMore}
        >
          <SlidersHorizontal className="h-3.75 w-3.75" />
          More filters
        </Button>

        {hasActiveFilters && (
          <Button
            variant="ghost"
            onClick={onClear}
            className="font-bold text-[var(--muted-c)] hover:text-[var(--ink)]"
          >
            <FilterX className="w-3.5 h-3.5" />
            Clear
          </Button>
        )}
      </div>

      {showMore && (
        <div className="mt-3 flex flex-wrap items-center gap-2.5 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4 shadow-sm animate-in fade-in slide-in-from-top-2">
          <PillSelect
            value={filters.donationTypeId ?? ''}
            onChange={(e) => onFilterChange('donationTypeId', e.target.value || undefined)}
            disabled={donationTypes.isPending}
            isActive={!!filters.donationTypeId}
          >
            <option value="">Donation Type: All</option>
            {donationTypes.data?.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </PillSelect>

          <PillSelect
            value={filters.donationFrequencyId ?? ''}
            onChange={(e) => onFilterChange('donationFrequencyId', e.target.value || undefined)}
            disabled={donationFrequencies.isPending}
            isActive={!!filters.donationFrequencyId}
          >
            <option value="">Frequency: All</option>
            {donationFrequencies.data?.map((frequency) => (
              <option key={frequency.id} value={frequency.id}>
                {frequency.name}
              </option>
            ))}
          </PillSelect>

          <div className="flex items-center gap-2">
            <span className="text-[12.5px] font-semibold text-[var(--muted-c)] ml-1">Follow-up before:</span>
            <input
              type="date"
              value={filters.followUpBefore ?? ''}
              onChange={(e) => onFilterChange('followUpBefore', e.target.value || undefined)}
              className={`h-[42px] rounded-full border px-3.5 text-[12.5px] font-semibold outline-none transition-colors ${
                filters.followUpBefore 
                  ? 'border-brand bg-brand text-primary-foreground' 
                  : 'border-[var(--border)] bg-[var(--card)] text-[var(--ink)] focus-visible:border-[var(--ink)]'
              }`}
            />
          </div>
        </div>
      )}
    </div>
  );
}
