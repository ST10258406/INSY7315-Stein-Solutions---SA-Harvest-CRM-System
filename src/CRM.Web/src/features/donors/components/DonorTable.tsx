import { useNavigate } from 'react-router-dom';
import { ArrowDown, ArrowUp, ArrowUpDown, Inbox, TriangleAlert } from 'lucide-react';
import { paths } from '@/routes/paths';
import type { ApiError, DonorListItemDto, DonorSortField } from '../types';
import { DonorStatusBadge } from './DonorStatusBadge';
import { formatDate, isOverdue, joinOrDash } from '../lib/donorFormatters';

interface Column {
  key: string;
  label: string;
  sortField?: DonorSortField;
  className?: string;
}

const COLUMNS: Column[] = [
  { key: 'company', label: 'Company', sortField: 'companyName' },
  { key: 'status', label: 'Status' },
  { key: 'region', label: 'Region' },
  { key: 'frequency', label: 'Donation frequency' },
  { key: 'rm', label: 'Relationship manager' },
  { key: 'followUp', label: 'Follow-up date', sortField: 'followUpDate' },
  { key: 'lastInteraction', label: 'Last interaction', sortField: 'lastInteractionDate' },
];

interface DonorTableProps {
  donors: DonorListItemDto[];
  isPending: boolean;
  isFetching?: boolean;
  isError: boolean;
  error: ApiError | null;
  hasActiveFilters: boolean;
  sortBy?: DonorSortField;
  sortDir?: 'asc' | 'desc';
  onSort: (field: DonorSortField) => void;
  onRetry: () => void;
}

export function DonorTable({
  donors,
  isPending,
  isFetching = false,
  isError,
  error,
  hasActiveFilters,
  sortBy,
  sortDir,
  onSort,
  onRetry,
}: DonorTableProps) {
  const navigate = useNavigate();

  return (
    <div
      className={`overflow-x-auto rounded-2xl border border-[#2B2B23] bg-[#141410] transition-opacity ${
        isFetching && !isPending ? 'opacity-60' : ''
      }`}
    >
      <table className="w-full min-w-[900px] border-collapse text-sm">
        <thead>
          <tr className="border-b border-[#2B2B23]">
            {COLUMNS.map((column) => (
              <th key={column.key} className="px-4 py-3 text-left">
                {column.sortField ? (
                  <button
                    type="button"
                    onClick={() => onSort(column.sortField!)}
                    className="flex items-center gap-1 text-xs font-medium tracking-wide text-[#B9B9AE] uppercase transition-colors hover:text-[#F4F4EE]"
                  >
                    {column.label}
                    <SortIcon active={sortBy === column.sortField} dir={sortDir} />
                  </button>
                ) : (
                  <span className="text-xs font-medium tracking-wide text-[#B9B9AE] uppercase">{column.label}</span>
                )}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {isPending ? (
            <SkeletonRows />
          ) : isError ? (
            <tr>
              <td colSpan={COLUMNS.length} className="px-4 py-12">
                <div className="flex flex-col items-center justify-center gap-2 text-center">
                  <TriangleAlert className="h-5 w-5 text-[#6B6B60]" />
                  <p className="text-sm font-medium text-[#B9B9AE]">Couldn't load donors</p>
                  <p className="text-xs text-[#6B6B60]">{error?.response?.data?.message ?? 'Something went wrong.'}</p>
                  <button
                    type="button"
                    onClick={onRetry}
                    className="mt-1 rounded-full border border-[#2B2B23] px-3 py-1 text-xs font-medium text-[#F4F4EE] transition-colors hover:border-[#F4F4EE]"
                  >
                    Retry
                  </button>
                </div>
              </td>
            </tr>
          ) : donors.length === 0 ? (
            <tr>
              <td colSpan={COLUMNS.length} className="px-4 py-12">
                <div className="flex flex-col items-center justify-center gap-2 text-center">
                  <Inbox className="h-5 w-5 text-[#6B6B60]" />
                  <p className="text-sm font-medium text-[#B9B9AE]">No donors found</p>
                  <p className="text-xs text-[#6B6B60]">
                    {hasActiveFilters ? 'Try adjusting or clearing your filters.' : 'Donors will appear here once added.'}
                  </p>
                </div>
              </td>
            </tr>
          ) : (
            donors.map((donor) => (
              <tr
                key={donor.id}
                onClick={() => navigate(paths.donorDetail(donor.id))}
                className="cursor-pointer border-b border-[#2B2B23] transition-colors last:border-b-0 hover:bg-[#1A1A14]"
              >
                <td className="px-4 py-3">
                  <div className="font-medium text-[#F4F4EE]">{donor.companyName}</div>
                  <div className="text-xs text-[#6B6B60]">{donor.companyType}</div>
                </td>
                <td className="px-4 py-3">
                  <DonorStatusBadge status={donor.status} />
                </td>
                <td className="px-4 py-3 text-[#B9B9AE]">{joinOrDash(donor.operationalRegions)}</td>
                <td className="px-4 py-3 text-[#B9B9AE]">{donor.donationFrequency ?? '—'}</td>
                <td className="px-4 py-3 text-[#B9B9AE]">{donor.relationshipManager?.fullName ?? 'Unassigned'}</td>
                <td className="px-4 py-3">
                  <span className={isOverdue(donor.followUpDate) ? 'font-medium text-rose-400' : 'text-[#B9B9AE]'}>
                    {formatDate(donor.followUpDate)}
                  </span>
                </td>
                <td className="px-4 py-3 text-[#B9B9AE]">
                  {formatDate(donor.lastInteractionDate)}
                  {donor.lastInteractionType && (
                    <span className="ml-1.5 text-xs text-[#6B6B60]">({donor.lastInteractionType})</span>
                  )}
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

function SortIcon({ active, dir }: { active: boolean; dir?: 'asc' | 'desc' }) {
  if (!active) return <ArrowUpDown className="h-3 w-3 opacity-50" />;
  return dir === 'desc' ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />;
}

function SkeletonRows() {
  return (
    <>
      {Array.from({ length: 8 }, (_, i) => (
        <tr key={i} className="border-b border-[#2B2B23] last:border-b-0">
          {COLUMNS.map((column) => (
            <td key={column.key} className="px-4 py-3.5">
              <div className="h-4 w-full max-w-32 animate-pulse rounded-md bg-[#26261D]" aria-hidden />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}
