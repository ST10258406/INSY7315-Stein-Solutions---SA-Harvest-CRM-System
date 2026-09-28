import { useState } from 'react';
import { Plus } from 'lucide-react';
import { useUsers, useUserListFilters, useSetUserActiveStatus } from './hooks';
import { UserFiltersBar } from './components/UserFiltersBar';
import { UserTable } from './components/UserTable';
import { UserPagination } from './components/UserPagination';
import { NewUserModal } from './components/NewUserModal';
import { EditUserModal } from './components/EditUserModal';
import { ChangeRoleModal } from './components/ChangeRoleModal';
import { DeactivateUserDialog } from './components/DeactivateUserDialog';
import { Button } from '@/components/ui/button';
import type { UserListItemDto } from './types';

export default function UserListPage() {
  const { filters, setSearch, setStatus, setRoleId, setSort, setPage, setPageSize } = useUserListFilters();
  const { data, isPending, isFetching, isError, error, refetch } = useUsers(filters);
  const setActiveStatus = useSetUserActiveStatus();

  const [newUserOpen, setNewUserOpen] = useState(false);
  const [editUser, setEditUser] = useState<UserListItemDto | null>(null);
  const [roleUser, setRoleUser] = useState<UserListItemDto | null>(null);
  const [deactivateUser, setDeactivateUser] = useState<UserListItemDto | null>(null);

  const hasActiveFilters = !!filters.search || filters.isActive !== undefined || !!filters.roleId;

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_34px]">
      <div className="mb-5 flex flex-wrap items-end gap-6">
        <div>
          <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Users</h1>
          <p className="m-0 text-sm font-medium text-[var(--muted-c)]">Manage internal staff accounts and permissions.</p>
        </div>

        <div className="ml-auto">
          <Button size="sm" onClick={() => setNewUserOpen(true)}>
            <Plus className="h-4 w-4 stroke-[2.2]" />
            <span>New User</span>
          </Button>
        </div>
      </div>

      <section className="flex flex-col gap-4 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4">
        <UserFiltersBar
          filters={filters}
          resultCount={data?.pagination.totalCount}
          onSearch={setSearch}
          onStatusChange={setStatus}
          onRoleChange={setRoleId}
        />

        <UserTable
          users={data?.data ?? []}
          isPending={isPending}
          isFetching={isFetching}
          isError={isError}
          error={error}
          hasActiveFilters={hasActiveFilters}
          sortBy={filters.sortBy}
          sortDir={filters.sortDir}
          onSort={setSort}
          onRetry={refetch}
          onEditDetails={setEditUser}
          onChangeRole={setRoleUser}
          onToggleStatus={(user) => {
            if (user.isActive) {
              setDeactivateUser(user);
            } else {
              // Reactivating needs no confirmation — it only restores access.
              setActiveStatus.mutate({ id: user.id, isActive: true });
            }
          }}
        />

        {data && data.pagination.totalCount > 0 && (
          <UserPagination pagination={data.pagination} onPageChange={setPage} onPageSizeChange={setPageSize} />
        )}
      </section>

      <NewUserModal open={newUserOpen} onOpenChange={setNewUserOpen} />
      <EditUserModal key={editUser?.id ?? 'edit'} user={editUser} onOpenChange={(open) => !open && setEditUser(null)} />
      <ChangeRoleModal key={roleUser?.id ?? 'role'} user={roleUser} onOpenChange={(open) => !open && setRoleUser(null)} />
      <DeactivateUserDialog user={deactivateUser} onOpenChange={(open) => !open && setDeactivateUser(null)} />
    </main>
  );
}
