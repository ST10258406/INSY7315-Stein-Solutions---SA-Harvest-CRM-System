import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, TriangleAlert } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { useChangeUserRole, useRoles } from '../hooks';
import { applyServerErrors } from '../lib/applyServerErrors';
import { changeRoleFormSchema, type ChangeRoleFormValues } from '../schemas';
import { RoleBadge } from './RoleBadge';
import type { UserListItemDto } from '../types';

interface ChangeRoleModalProps {
  user: UserListItemDto | null;
  onOpenChange: (open: boolean) => void;
}

// Only SuperAdmin may change a user's role — enforced by the API
// (PATCH /api/v1/users/{id}/role requires the SuperAdminOnly policy). The
// frontend check here is purely cosmetic; it just avoids a round trip that
// would 403 for anyone who isn't SuperAdmin.
export function ChangeRoleModal({ user, onOpenChange }: ChangeRoleModalProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const roles = useRoles();
  const changeUserRole = useChangeUserRole();
  const isSuperAdmin = (useAuthStore((s) => s.user?.roles) ?? []).includes('SuperAdmin');

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ChangeRoleFormValues>({
    resolver: zodResolver(changeRoleFormSchema),
    // The parent remounts this component (key={user.id}) each time a different
    // user is selected, so this default is always fresh.
    defaultValues: { roleId: user?.roleId ?? '' },
  });

  const handleOpenChange = (next: boolean) => {
    if (!next) onOpenChange(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (!user) return;
    setGeneralError(null);
    try {
      await changeUserRole.mutateAsync({ id: user.id, request: values });
      onOpenChange(false);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) {
        setGeneralError('Could not update this user\'s role. Please try again.');
      }
    }
  });

  return (
    <Dialog open={!!user} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Change Role</DialogTitle>
          <DialogDescription>Update permissions for {user ? `${user.firstName} ${user.lastName}` : 'this user'}.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          <div className="flex items-center gap-2.5">
            <span className="text-[12.5px] font-semibold text-muted-foreground">Current role:</span>
            {user && <RoleBadge role={user.role} />}
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="change-role-select" className="text-xs font-bold text-foreground">
              New role
            </label>
            <select
              id="change-role-select"
              {...register('roleId')}
              disabled={roles.isPending}
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 disabled:opacity-50 ${
                errors.roleId ? 'border-destructive/70' : 'border-border'
              }`}
            >
              <option value="">{roles.isPending ? 'Loading…' : 'Select a role'}</option>
              {roles.data?.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </select>
            {errors.roleId && <span className="text-xs text-destructive">{errors.roleId.message}</span>}
          </div>

          <div className="flex items-start gap-2.5 rounded-2xl border border-amber-400/40 bg-amber-400/10 px-3.5 py-3">
            <TriangleAlert className="mt-0.5 h-3.75 w-3.75 shrink-0 text-amber-600 dark:text-amber-400" />
            <span className="text-xs font-semibold text-amber-700 dark:text-amber-400">This action requires SuperAdmin permission</span>
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting || !isSuperAdmin} title={isSuperAdmin ? undefined : 'Requires SuperAdmin permission'}>
              {isSubmitting ? 'Saving…' : 'Save'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
