import { useNavigate } from 'react-router-dom';
import { 
  ArrowDown, ArrowUp, ArrowUpDown, FilterX, TriangleAlert, 
  MoreHorizontal, Eye, Edit3, Phone, Mail, Users 
} from 'lucide-react';
import { paths } from '@/routes/paths';
import type { ApiError, DonorListItemDto, DonorSortField } from '../types';
import { formatDate, isOverdue } from '../lib/donorFormatters';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

function getInitials(name: string) {
  if (!name) return '??';
  const parts = name.split(' ');
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  return name.substring(0, 2).toUpperCase();
}

const MANAGER_PALETTE = [
  '#3F5D46', '#4B5267', '#6A4C3D', '#544766', '#59604C', '#684551', '#425A63'
];

const getStatusClasses = (status: string) => {
  switch (status) {
    case 'Pending Review':
    case 'PendingReview':
      return { text: 'text-amber-700 dark:text-amber-400', dot: 'bg-amber-500' };
    case 'Active':
      return { text: 'text-emerald-700 dark:text-emerald-400', dot: 'bg-emerald-500' };
    case 'Lapsed':
      return { text: 'text-rose-700 dark:text-rose-400', dot: 'bg-rose-500' };
    default:
      return { text: 'text-muted-foreground', dot: 'bg-muted-foreground' };
  }
};

const formatStatusText = (status: string) => {
  if (status === 'PendingReview') return 'Pending review';
  return status;
};

const renderLastInteractionIcon = (type: string | undefined | null) => {
  switch (type?.toLowerCase()) {
    case 'email':
      return <Mail className="w-3.75 h-3.75 text-muted-foreground" />;
    case 'meeting':
      return <Users className="w-3.75 h-3.75 text-muted-foreground" />;
    default:
      return <Phone className="w-3.75 h-3.75 text-muted-foreground" />;
  }
};

const MENU_ITEM_CLASSES = 'h-9 gap-2.5 px-3 text-13 font-semibold text-[var(--ink)] cursor-pointer whitespace-nowrap';

interface Column {
  key: string;
  label: string;
  sortField?: DonorSortField;
  className?: string;
}

const COLUMNS: Column[] = [
  { key: 'company', label: 'COMPANY NAME', sortField: 'companyName', className: 'text-left py-3.75 pr-3.5 pl-5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'type', label: 'COMPANY TYPE', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'status', label: 'STATUS', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'rm', label: 'RELATIONSHIP MANAGER', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'region', label: 'REGIONS', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'frequency', label: 'DONATION FREQUENCY', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'lastInteraction', label: 'LAST INTERACTION', sortField: 'lastInteractionDate', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
  { key: 'followUp', label: 'FOLLOW-UP DATE', sortField: 'followUpDate', className: 'text-left py-3.75 px-3.5 text-[11.5px] font-bold tracking-wider text-[var(--muted2)]' },
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
    <div className={`bg-[var(--card)] rounded-xl shadow-[0_1px_3px_var(--shadow)] overflow-visible ${isFetching && !isPending ? 'opacity-60' : ''}`}>
      <div className="overflow-x-auto">
        <table className="w-full border-collapse min-w-[1180px]">
          <thead>
            <tr className="bg-[var(--hair)]">
              {COLUMNS.map((column) => (
                <th key={column.key} className={column.className}>
                  {column.sortField ? (
                    <button
                      type="button"
                      onClick={() => onSort(column.sortField!)}
                      className="flex items-center gap-1 font-bold uppercase transition-colors hover:text-[var(--ink)]"
                    >
                      {column.label}
                      <SortIcon active={sortBy === column.sortField} dir={sortDir} />
                    </button>
                  ) : (
                    <span className="font-bold uppercase">
                      {column.label}
                    </span>
                  )}
                </th>
              ))}
              <th className="w-14 py-3.75 pr-4 pl-2 bg-[var(--hair)]"></th>
            </tr>
          </thead>
          <tbody>
            {isPending ? (
              <SkeletonRows />
            ) : isError ? (
              <tr>
                <td colSpan={COLUMNS.length + 1} className="px-4 py-16">
                  <div className="flex flex-col items-center justify-center gap-2 text-center">
                    <TriangleAlert className="h-5 w-5 text-[var(--muted-c)]" />
                    <p className="m-0 text-sm font-medium text-[var(--ink)]">Couldn't load donors</p>
                    <p className="m-0 text-xs text-[var(--muted-c)]">
                      {error?.response?.data?.message ?? 'Something went wrong.'}
                    </p>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={onRetry}
                      className="mt-1 border-[var(--border)] text-[var(--ink)] hover:border-[var(--ink)]"
                    >
                      Retry
                    </Button>
                  </div>
                </td>
              </tr>
            ) : donors.length === 0 ? (
              <tr>
                <td colSpan={COLUMNS.length + 1} className="px-4 py-16">
                  <div className="flex flex-col items-center text-center py-4 px-6">
                    <div className="w-14.5 h-14.5 rounded-2xl bg-[var(--icon-bg)] flex items-center justify-center mb-4.5">
                      <FilterX className="w-6.5 h-6.5 text-[var(--icon)]" />
                    </div>
                    <h3 className="m-0 mb-1.75 text-lg font-extrabold tracking-tight text-[var(--ink)]">
                      No donors found
                    </h3>
                    <p className="m-0 max-w-[340px] text-[13.5px] font-medium text-[var(--muted-c)]">
                      {hasActiveFilters
                        ? 'No donor records match the filters you have applied. Try widening your search.'
                        : 'Donors will appear here once added.'}
                    </p>
                  </div>
                </td>
              </tr>
            ) : (
              donors.map((donor, idx) => {
                const s = getStatusClasses(donor.status);
                const rmName = donor.relationshipManager?.fullName ?? 'Unassigned';
                const initials = getInitials(rmName);
                const avatarBg = rmName !== 'Unassigned' ? MANAGER_PALETTE[idx % MANAGER_PALETTE.length] : '#3B4A40';
                
                const shownRegions = donor.operationalRegions.slice(0, 2);
                const extraRegions = donor.operationalRegions.length - shownRegions.length;

                return (
                  <tr
                    key={donor.id}
                    onClick={() => navigate(paths.donorDetail(donor.id))}
                    className="border-t border-[var(--hair)] cursor-pointer hover:bg-[var(--row-hover)] transition-colors"
                  >
                    <td className="py-3.75 pr-3.5 pl-5">
                      <span className="text-[13.5px] font-bold text-[var(--ink)] hover:text-brand whitespace-nowrap">
                        {donor.companyName}
                      </span>
                    </td>

                    <td className="py-3.75 px-3.5 text-13 text-[var(--muted-c)] font-medium whitespace-nowrap">
                      {donor.companyType}
                    </td>

                    <td className="py-3.75 px-3.5">
                      <span
                        className={`inline-flex items-center gap-1.75 text-[12.5px] font-bold whitespace-nowrap ${s.text}`}
                      >
                        <span
                          className={`w-1.5 h-1.5 rounded-full ${s.dot}`}
                        />
                        <span>{formatStatusText(donor.status)}</span>
                      </span>
                    </td>

                    <td className="py-3.75 px-3.5">
                      <div className="flex items-center gap-2.25">
                        {rmName !== 'Unassigned' ? (
                          <>
                            <div
                              className="shrink-0 w-7 h-7 rounded-full text-white text-[10px] font-bold flex items-center justify-center"
                              style={{ backgroundColor: avatarBg }}
                            >
                              {initials}
                            </div>
                            <span className="text-13 font-semibold text-[var(--ink)] whitespace-nowrap">
                              {rmName}
                            </span>
                          </>
                        ) : (
                          <span className="text-13 text-[var(--muted-c)] whitespace-nowrap">—</span>
                        )}
                      </div>
                    </td>

                    <td className="py-3.75 px-3.5">
                      <div className="flex items-center gap-1.25">
                        {shownRegions.map((region) => (
                          <span
                            key={region}
                            className="px-2.25 py-1 rounded-lg bg-[var(--chip)] text-[var(--ink)] text-[11px] font-bold tracking-tight whitespace-nowrap"
                          >
                            {region}
                          </span>
                        ))}
                        {extraRegions > 0 && (
                          <span className="px-2 py-1 text-[11px] font-semibold text-[var(--muted2)] whitespace-nowrap">
                            +{extraRegions} more
                          </span>
                        )}
                        {shownRegions.length === 0 && <span className="text-[13px] text-[var(--muted-c)]">—</span>}
                      </div>
                    </td>

                    <td className="py-3.75 px-3.5 text-13 text-[var(--muted-c)] font-medium whitespace-nowrap">
                      {donor.donationFrequency ?? '—'}
                    </td>

                    <td className="py-3.75 px-3.5">
                      {donor.lastInteractionDate ? (
                        <div className="flex items-center gap-1.75 whitespace-nowrap">
                          {renderLastInteractionIcon(donor.lastInteractionType)}
                          <span className="text-13 text-[var(--muted-c)] font-medium">
                            {formatDate(donor.lastInteractionDate)}
                          </span>
                        </div>
                      ) : (
                        <span className="text-[13px] text-[var(--muted-c)]">—</span>
                      )}
                    </td>

                    <td className="py-3.75 px-3.5 text-13 font-semibold whitespace-nowrap">
                      <span className={isOverdue(donor.followUpDate) ? 'text-destructive' : 'text-[var(--ink)]'}>
                        {donor.followUpDate ? formatDate(donor.followUpDate) : '—'}
                      </span>
                    </td>

                    {/* Row Action Menu — a portalled DropdownMenu, so it renders above every
                        row instead of inside this cell's stacking context (where the next
                        row's "…" button used to paint over it). */}
                    <td className="py-3.75 pr-4 pl-2 text-right" onClick={(e) => e.stopPropagation()}>
                      <DropdownMenu>
                        <DropdownMenuTrigger
                          render={
                            <Button
                              variant="secondary"
                              size="icon"
                              title="Row actions"
                              aria-label={`Actions for ${donor.companyName}`}
                              className="ml-auto hover:bg-[var(--hover)]"
                            >
                              <MoreHorizontal className="w-4 h-4 text-[var(--icon)]" />
                            </Button>
                          }
                        />
                        {/* stopPropagation: React bubbles portal events through the component
                            tree, so a click here would otherwise also hit the row's onClick. */}
                        <DropdownMenuContent align="end" className="w-52 p-1.5" onClick={(e) => e.stopPropagation()}>
                          <DropdownMenuItem className={MENU_ITEM_CLASSES} onClick={() => navigate(paths.donorDetail(donor.id))}>
                            <Eye className="w-3.75 h-3.75 text-[var(--icon)] shrink-0" />
                            <span>View Profile</span>
                          </DropdownMenuItem>
                          <DropdownMenuItem className={MENU_ITEM_CLASSES} onClick={() => navigate(paths.donorEdit(donor.id))}>
                            <Edit3 className="w-3.75 h-3.75 text-[var(--icon)] shrink-0" />
                            <span>Edit Donor</span>
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
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
        <tr key={i} className="border-t border-[var(--hair)]">
          <td className="py-4 pr-3.5 pl-5">
            <div className="w-[158px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 px-3.5">
            <div className="w-[92px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 px-3.5">
            <div className="w-[86px] h-5.5 rounded-full bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 px-3.5">
            <div className="flex items-center gap-2.5">
              <div className="w-7 h-7 rounded-full bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
              <div className="w-[102px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
            </div>
          </td>
          <td className="py-4 px-3.5">
            <div className="w-[70px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 px-3.5">
            <div className="w-[74px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 px-3.5">
            <div className="w-[88px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 px-3.5">
            <div className="w-[84px] h-3 rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)] animate-shimmer" />
          </td>
          <td className="py-4 pr-4 pl-2" />
        </tr>
      ))}
    </>
  );
}
