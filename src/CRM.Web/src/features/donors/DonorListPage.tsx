import { Link } from 'react-router-dom';
import { Plus } from 'lucide-react';
import { paths } from '@/routes/paths';
import { useDonors } from './hooks';
import { useDonorListFilters } from './hooks/useDonorListFilters';
import { DonorFiltersBar } from './components/DonorFiltersBar';
import { DonorTable } from './components/DonorTable';
import { DonorPagination } from './components/DonorPagination';

export default function DonorListPage() {
  const { filters, setFilter, setSort, setPage, setPageSize, clearFilters } = useDonorListFilters();
  const { data, isPending, isFetching, isError, error, refetch } = useDonors(filters);

  const hasActiveFilters =
    !!filters.search ||
    !!filters.status ||
    !!filters.companyTypeId ||
    !!filters.regionCode ||
    !!filters.donationTypeId ||
    !!filters.donationFrequencyId ||
    !!filters.followUpBefore;

  return (
    <div className="flex flex-col gap-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-[#F4F4EE]">Donors</h1>
          <p className="mt-1 text-sm text-[#B9B9AE]">
            {isPending ? 'Loading donors…' : `${data?.pagination.totalCount ?? 0} donor${data?.pagination.totalCount === 1 ? '' : 's'}`}
          </p>
        </div>
        <Link
          to={paths.donorNew}
          className="flex h-9 items-center gap-1.5 rounded-full bg-brand px-4 text-[13px] font-bold text-[#16160F] transition-opacity hover:opacity-90"
        >
          <Plus className="h-4 w-4" />
          New donor
        </Link>
      </div>

      <DonorFiltersBar filters={filters} onFilterChange={setFilter} onClear={clearFilters} />

      <DonorTable
        donors={data?.data ?? []}
        isPending={isPending}
        isFetching={isFetching}
        isError={isError}
        error={error}
        hasActiveFilters={hasActiveFilters}
        sortBy={filters.sortBy}
        sortDir={filters.sortDir}
        onSort={setSort}
        onRetry={refetch}
      />

      {data && data.pagination.totalCount > 0 && (
        <DonorPagination pagination={data.pagination} onPageChange={setPage} onPageSizeChange={setPageSize} />
      )}
    </div>
  );
}
