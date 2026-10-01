import { useState } from 'react';
import { ArrowDown, ArrowUp, ArrowUpDown, FilterX, TriangleAlert, MoreHorizontal, Pencil, Shield, UserX, UserCheck, Lock, LockOpen } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { RoleBadge } from './RoleBadge';
import type { ApiError, UserListItemDto, UserSortField } from '../types';

function getInitials(firstName: string, lastName: string) {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase() || '??';
}

const AVATAR_PALETTE = ['#3B4A40', '#3F5D46', '#5B4B8A', '#8A5A2B', '#2E5A78', '#7A3B4E', '#4A5B2E', '#6B4A2E'];

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('en-ZA', { hour: '2-digit', minute: '2-digit' });
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });
}

interface Column {
  key: string;
  label: string;
  sortField?: UserSortField;
}

const COLUMNS: Column[] = [
  { key: 'name', label: 'NAME', sortField: 'name' },
  { key: 'email', label: 'EMAIL', sortField: 'email' },
  { key: 'role', label: 'ROLE' },
  { key: 'status', label: 'STATUS' },
  { key: 'created', label: 'CREATED', sortField: 'createdAt' },
];

interface UserTableProps {
  users: UserListItemDto[];
  isPending: boolean;
  isFetching?: boolean;
  isError: boolean;
  error: ApiError | null;
  hasActiveFilters: boolean;
  sortBy?: UserSortField;
  sortDir?: 'asc' | 'desc';
  onSort: (field: UserSortField) => void;
  onRetry: () => void;
  onEditDetails: (user: UserListItemDto) => void;
  onChangeRole: (user: UserListItemDto) => void;
  onToggleStatus: (user: UserListItemDto) => void;
  onUnlock: (user: UserListItemDto) => void;
}

export function UserTable({
  users,
  isPending,
  isFetching = false,
  isError,
  error,
  hasActiveFilters,
  sortBy,
  sortDir,
  onSort,
  onRetry,
  onEditDetails,
  onChangeRole,
  onToggleStatus,
  onUnlock,
}: UserTableProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const currentUser = useAuthStore((s) => s.user);
  const isSuperAdmin = (currentUser?.roles ?? []).includes('SuperAdmin');

  return (
    <div className={`overflow-visible rounded-xl bg-[var(--card)] shadow-[0_1px_3px_var(--shadow)] ${isFetching && !isPending ? 'opacity-60' : ''}`}>
      <div className="-mb-16 overflow-x-auto overflow-y-visible pb-16">
        <table className="w-full min-w-[820px] border-collapse">
          <thead>
            <tr className="bg-[var(--hair)]">
              {COLUMNS.map((column) => (
                <th key={column.key} className="py-3.75 px-3.5 text-left text-[11.5px] font-bold tracking-wider text-[var(--muted2)] first:pl-5">
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
                    <span className="font-bold uppercase">{column.label}</span>
                  )}
                </th>
              ))}
              <th className="w-14 bg-[var(--hair)] py-3.75 pr-4 pl-2" />
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
                    <p className="m-0 text-sm font-medium text-[var(--ink)]">Couldn't load users</p>
                    <p className="m-0 text-xs text-[var(--muted-c)]">{error?.response?.data?.message ?? 'Something went wrong.'}</p>
                    <Button variant="outline" size="sm" onClick={onRetry} className="mt-1 border-[var(--border)] text-[var(--ink)] hover:border-[var(--ink)]">
                      Retry
                    </Button>
                  </div>
                </td>
              </tr>
            ) : users.length === 0 ? (
              <tr>
                <td colSpan={COLUMNS.length + 1} className="px-4 py-16">
                  <div className="flex flex-col items-center px-6 py-4 text-center">
                    <div className="mb-4.5 flex h-14.5 w-14.5 items-center justify-center rounded-2xl bg-[var(--icon-bg)]">
                      <FilterX className="h-6.5 w-6.5 text-[var(--icon)]" />
                    </div>
                    <h3 className="m-0 mb-1.75 text-lg font-extrabold tracking-tight text-[var(--ink)]">No users found</h3>
                    <p className="m-0 max-w-[340px] text-[13.5px] font-medium text-[var(--muted-c)]">
                      {hasActiveFilters
                        ? 'No staff accounts match the filters you have applied. Try widening your search.'
                        : 'Staff accounts will appear here once added.'}
                    </p>
                  </div>
                </td>
              </tr>
            ) : (
              users.map((user, idx) => {
                const isSelf = user.id === currentUser?.id;
                // Only a SuperAdmin may edit or change the status of a SuperAdmin — enforced
                // server-side (UserTargetAuthorizationHandler); mirrored here so Admins aren't
                // offered actions that would just 403.
                const isProtected = user.role === 'SuperAdmin' && !isSuperAdmin;
                const protectedTitle = 'Only a SuperAdmin can manage a SuperAdmin';
                const canDeactivate = !isSelf && !isProtected;

                return (
                  <tr key={user.id} className="border-t border-[var(--hair)] transition-colors hover:bg-[var(--row-hover)]">
                    <td className="py-3.75 pr-3.5 pl-5">
                      <div className="flex items-center gap-2.5">
                        <div
                          className="flex h-8.5 w-8.5 shrink-0 items-center justify-center rounded-full text-[11.5px] font-bold text-white"
                          style={{ backgroundColor: AVATAR_PALETTE[idx % AVATAR_PALETTE.length] }}
                        >
                          {getInitials(user.firstName, user.lastName)}
                        </div>
                        <span className="whitespace-nowrap text-[13.5px] font-bold text-[var(--ink)]">
                          {user.firstName} {user.lastName}
                        </span>
                        {isSelf && (
                          <span className="rounded-2xl bg-[var(--icon-bg)] px-2 py-0.5 text-[10px] font-bold whitespace-nowrap text-[var(--muted-c)]">
                            You
                          </span>
                        )}
                      </div>
                    </td>

                    <td className="text-13 px-3.5 py-3.75 font-medium whitespace-nowrap text-[var(--muted-c)]">{user.email}</td>

                    <td className="px-3.5 py-3.75">
                      <RoleBadge role={user.role} />
                    </td>

                    <td className="px-3.5 py-3.75">
                      <span className="inline-flex items-center gap-1.75 text-[12.5px] font-bold whitespace-nowrap">
                        <span className={`h-1.5 w-1.5 rounded-full ${user.isActive ? 'bg-emerald-500' : 'bg-[var(--muted2)]'}`} />
                        <span className={user.isActive ? 'text-emerald-700 dark:text-emerald-400' : 'text-[var(--muted2)]'}>
                          {user.isActive ? 'Active' : 'Deactivated'}
                        </span>
                      </span>
                      {user.isLockedOut && (
                        <span
                          title={user.lockedUntil ? `Too many failed sign-ins — locked until ${formatTime(user.lockedUntil)}` : 'Too many failed sign-ins'}
                          className="mt-1 flex w-fit items-center gap-1 rounded-2xl bg-amber-500/15 px-2 py-0.5 text-[10.5px] font-bold whitespace-nowrap text-amber-700 dark:text-amber-400"
                        >
                          <Lock className="h-3 w-3 shrink-0" />
                          {user.lockedUntil ? `Locked until ${formatTime(user.lockedUntil)}` : 'Locked'}
                        </span>
                      )}
                    </td>

                    <td className="text-13 px-3.5 py-3.75 font-medium whitespace-nowrap text-[var(--muted-c)]">{formatDate(user.createdAt)}</td>

                    <td className="relative bg-[var(--card)] py-3.75 pr-4 pl-2 text-right">
                      <Button
                        variant="secondary"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation();
                          setOpenMenuId(openMenuId === user.id ? null : user.id);
                        }}
                        title="Actions"
                        className="ml-auto hover:bg-[var(--hover)]"
                      >
                        <MoreHorizontal className="h-4 w-4 text-[var(--icon)]" />
                      </Button>

                      {openMenuId === user.id && (
                        <div
                          onClick={(e) => e.stopPropagation()}
                          className="absolute top-[46px] right-3.5 z-20 flex w-52 flex-col gap-0.5 rounded-xl border border-[var(--border)] bg-[var(--card)] p-1.5 text-left shadow-[0_12px_28px_rgba(20,20,15,0.16)]"
                        >
                          <button
                            disabled={isProtected}
                            title={isProtected ? protectedTitle : undefined}
                            onClick={() => {
                              setOpenMenuId(null);
                              if (isProtected) return;
                              onEditDetails(user);
                            }}
                            className={`flex h-9 items-center gap-2.5 rounded-lg border-none bg-transparent px-3 text-[12.5px] font-semibold whitespace-nowrap ${
                              isProtected ? 'cursor-not-allowed text-[var(--muted2)]' : 'text-[var(--ink)] hover:bg-[var(--hover)]'
                            }`}
                          >
                            <Pencil className="h-3.75 w-3.75 shrink-0 text-[var(--icon)]" />
                            <span>Edit Details</span>
                          </button>

                          <button
                            title={isSuperAdmin ? undefined : 'Requires SuperAdmin permission'}
                            onClick={() => {
                              setOpenMenuId(null);
                              onChangeRole(user);
                            }}
                            className="flex h-9 items-center gap-2.5 rounded-lg border-none bg-transparent px-3 text-[12.5px] font-semibold whitespace-nowrap text-[var(--ink)] hover:bg-[var(--hover)]"
                          >
                            <Shield className="h-3.75 w-3.75 shrink-0 text-[var(--icon)]" />
                            <span>Change Role</span>
                          </button>

                          {user.isLockedOut && (
                            <button
                              disabled={isProtected}
                              title={isProtected ? protectedTitle : undefined}
                              onClick={() => {
                                setOpenMenuId(null);
                                if (isProtected) return;
                                onUnlock(user);
                              }}
                              className={`flex h-9 items-center gap-2.5 rounded-lg border-none bg-transparent px-3 text-[12.5px] font-semibold whitespace-nowrap ${
                                isProtected ? 'cursor-not-allowed text-[var(--muted2)]' : 'text-[var(--ink)] hover:bg-[var(--hover)]'
                              }`}
                            >
                              <LockOpen className="h-3.75 w-3.75 shrink-0 text-[var(--icon)]" />
                              <span>Unlock Account</span>
                            </button>
                          )}

                          <div className="mx-1.5 my-1 h-px bg-[var(--hair)]" />

                          <button
                            disabled={!canDeactivate}
                            title={isSelf ? 'You cannot deactivate your own account' : isProtected ? protectedTitle : undefined}
                            onClick={() => {
                              setOpenMenuId(null);
                              if (!canDeactivate) return;
                              onToggleStatus(user);
                            }}
                            className={`flex h-9 items-center gap-2.5 rounded-lg border-none bg-transparent px-3 text-[12.5px] font-semibold whitespace-nowrap ${
                              !canDeactivate
                                ? 'cursor-not-allowed text-[var(--muted2)]'
                                : user.isActive
                                  ? 'cursor-pointer text-[var(--brand-red,#D4373A)] hover:bg-rose-500/10'
                                  : 'cursor-pointer text-emerald-600 hover:bg-emerald-500/10'
                            }`}
                          >
                            {user.isActive ? <UserX className="h-3.75 w-3.75 shrink-0" /> : <UserCheck className="h-3.75 w-3.75 shrink-0" />}
                            <span>{user.isActive ? 'Deactivate' : 'Reactivate'}</span>
                          </button>
                        </div>
                      )}
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
            <div className="flex items-center gap-2.5">
              <div className="h-8.5 w-8.5 animate-shimmer rounded-full bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)]" />
              <div className="h-3 w-[132px] animate-shimmer rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)]" />
            </div>
          </td>
          <td className="px-3.5 py-4">
            <div className="h-3 w-[150px] animate-shimmer rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)]" />
          </td>
          <td className="px-3.5 py-4">
            <div className="h-5.5 w-[86px] animate-shimmer rounded-full bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)]" />
          </td>
          <td className="px-3.5 py-4">
            <div className="h-3 w-[70px] animate-shimmer rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)]" />
          </td>
          <td className="px-3.5 py-4">
            <div className="h-3 w-[88px] animate-shimmer rounded-md bg-gradient-to-r from-[var(--skel)] via-[var(--skel2)] to-[var(--skel)]" />
          </td>
          <td className="py-4 pr-4 pl-2" />
        </tr>
      ))}
    </>
  );
}
