import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Download, Send } from 'lucide-react';
import { paths } from '@/routes/paths';
import { useDonors, useExportDonors } from './hooks';
import { useDonorListFilters } from './hooks/useDonorListFilters';
import { DonorFiltersBar } from './components/DonorFiltersBar';
import { DonorTable } from './components/DonorTable';
import { DonorPagination } from './components/DonorPagination';
import { SendPublicFormInviteDialog } from './components/SendPublicFormInviteDialog';
import { Button, buttonVariants } from '@/components/ui/button';

export default function DonorListPage() {
  const { filters, setFilter, setSort, setPage, setPageSize, clearFilters } = useDonorListFilters();
  const { data, isPending, isFetching, isError, error, refetch } = useDonors(filters);
  const [inviteOpen, setInviteOpen] = useState(false);
  const exportDonors = useExportDonors();

  const hasActiveFilters =
    !!filters.search ||
    !!filters.status ||
    !!filters.companyTypeId ||
    !!filters.regionCode ||
    !!filters.donationTypeId ||
    !!filters.donationFrequencyId ||
    !!filters.followUpBefore;

  return (
    <main className="flex-1 min-w-0 overflow-y-auto p-[26px_30px_34px]">
      {/* Header Banner */}
      <div className="flex items-end gap-6 flex-wrap mb-5">
        <div>
          <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">
            Donors
          </h1>
          <p className="m-0 text-sm font-medium text-[var(--muted-c)]">
            {isPending ? 'Loading donors…' : `${data?.pagination.totalCount ?? 0} donor${data?.pagination.totalCount === 1 ? '' : 's'} total`}
          </p>
        </div>

        <div className="ml-auto flex items-center gap-2.5">
          <Button
            variant="secondary"
            size="sm"
            disabled={exportDonors.isPending || isPending || !data?.pagination.totalCount}
            onClick={() => exportDonors.mutate(filters)}
            title={hasActiveFilters ? 'Export donors matching the current filters (CSV)' : 'Export all donors (CSV)'}
          >
            <Download className="w-3.75 h-3.75" />
            <span>{exportDonors.isPending ? 'Exporting…' : 'Export'}</span>
          </Button>

          <Button variant="secondary" size="sm" onClick={() => setInviteOpen(true)}>
            <Send className="w-3.75 h-3.75" />
            <span>Send Public Form</span>
          </Button>

          <Link
            to={paths.donorNew}
            className={buttonVariants({ variant: 'default', size: 'sm' })}
          >
            <Plus className="w-4 h-4 stroke-[2.2]" />
            <span>New Donor</span>
          </Link>
        </div>
      </div>

      <DonorFiltersBar filters={filters} onFilterChange={setFilter} onClear={clearFilters} />

      {/* Main Table Section */}
      <section className="bg-[var(--soft)] border border-[var(--border)] rounded-2xl p-4 flex flex-col gap-4">
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
      </section>

      <SendPublicFormInviteDialog open={inviteOpen} onOpenChange={setInviteOpen} />
    </main>
  );
}
