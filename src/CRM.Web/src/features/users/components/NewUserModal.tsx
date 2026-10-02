import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, Check, Copy } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { useCreateUser, useRoles } from '../hooks';
import { applyServerErrors } from '../lib/applyServerErrors';
import { newUserFormSchema, defaultNewUserValues, type NewUserFormValues } from '../schemas';
import type { CreateUserResponseDto } from '../types';

interface NewUserModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function NewUserModal({ open, onOpenChange }: NewUserModalProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreateUserResponseDto | null>(null);
  const [copied, setCopied] = useState(false);
  const roles = useRoles();
  const createUser = useCreateUser();
  const isSuperAdmin = (useAuthStore((s) => s.user?.roles) ?? []).includes('SuperAdmin');
  // Only a SuperAdmin may assign the SuperAdmin role (enforced server-side by
  // CreateUserAuthorizationFilter) — hide the option here so an Admin never submits a
  // request that's guaranteed to come back as a 403.
  const assignableRoles = roles.data?.filter((role) => isSuperAdmin || role.name !== 'SuperAdmin');

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<NewUserFormValues>({
    resolver: zodResolver(newUserFormSchema),
    defaultValues: defaultNewUserValues,
  });

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset(defaultNewUserValues);
      setGeneralError(null);
      setCreated(null);
      setCopied(false);
      createUser.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setGeneralError(null);
    try {
      const result = await createUser.mutateAsync(values);
      setCreated(result);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) {
        setGeneralError('Could not create this user. Please try again.');
      }
    }
  });

  const copyPassword = async () => {
    if (!created) return;
    try {
      await navigator.clipboard.writeText(created.temporaryPassword);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Clipboard API can be unavailable (e.g. insecure context) — the password is
      // still visible on screen for the admin to copy manually.
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange} disablePointerDismissal>
      <DialogContent>
        {created ? (
          <>
            <DialogHeader>
              <DialogTitle>User created</DialogTitle>
              <DialogDescription>
                Share this temporary password with {created.firstName} {created.lastName} — it will not be shown again.
                They will need it to log in for the first time.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-2">
              <span className="text-xs font-bold text-foreground">Temporary password</span>
              <div className="flex gap-2">
                <div className="flex h-11 flex-1 items-center rounded-2xl border border-dashed border-border bg-muted px-3.75 font-mono text-[13.5px] font-bold tracking-wide text-foreground">
                  {created.temporaryPassword}
                </div>
                <Button type="button" variant="secondary" onClick={copyPassword}>
                  {copied ? <Check className="h-3.75 w-3.75" /> : <Copy className="h-3.75 w-3.75" />}
                  <span>{copied ? 'Copied' : 'Copy'}</span>
                </Button>
              </div>
            </div>

            <DialogFooter>
              <Button type="button" onClick={() => handleOpenChange(false)}>
                Done
              </Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>New User</DialogTitle>
              <DialogDescription>Create an internal staff account.</DialogDescription>
            </DialogHeader>

            <form onSubmit={onSubmit} className="flex flex-col gap-3">
              {generalError && (
                <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
                  <AlertCircle className="h-3.75 w-3.75 shrink-0" />
                  <span>{generalError}</span>
                </div>
              )}

              <div className="grid grid-cols-2 gap-3">
                <div className="flex flex-col gap-2">
                  <label htmlFor="new-user-first-name" className="text-xs font-bold text-foreground">
                    First Name
                  </label>
                  <input
                    id="new-user-first-name"
                    {...register('firstName')}
                    placeholder="First name"
                    className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                      errors.firstName ? 'border-destructive/70' : 'border-border'
                    }`}
                  />
                  {errors.firstName && <span className="text-xs text-destructive">{errors.firstName.message}</span>}
                </div>

                <div className="flex flex-col gap-2">
                  <label htmlFor="new-user-last-name" className="text-xs font-bold text-foreground">
                    Last Name
                  </label>
                  <input
                    id="new-user-last-name"
                    {...register('lastName')}
                    placeholder="Last name"
                    className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                      errors.lastName ? 'border-destructive/70' : 'border-border'
                    }`}
                  />
                  {errors.lastName && <span className="text-xs text-destructive">{errors.lastName.message}</span>}
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <label htmlFor="new-user-email" className="text-xs font-bold text-foreground">
                  Email
                </label>
                <input
                  id="new-user-email"
                  type="email"
                  {...register('email')}
                  placeholder="name@saharvest.org"
                  className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                    errors.email ? 'border-destructive/70' : 'border-border'
                  }`}
                />
                {errors.email && <span className="text-xs text-destructive">{errors.email.message}</span>}
              </div>

              <div className="flex flex-col gap-2">
                <label htmlFor="new-user-role" className="text-xs font-bold text-foreground">
                  Role
                </label>
                <select
                  id="new-user-role"
                  {...register('roleId')}
                  disabled={roles.isPending}
                  className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 disabled:opacity-50 ${
                    errors.roleId ? 'border-destructive/70' : 'border-border'
                  }`}
                >
                  <option value="">{roles.isPending ? 'Loading…' : 'Select a role'}</option>
                  {assignableRoles?.map((role) => (
                    <option key={role.id} value={role.id}>
                      {role.name}
                    </option>
                  ))}
                </select>
                {errors.roleId && <span className="text-xs text-destructive">{errors.roleId.message}</span>}
              </div>

              <span className="text-[11.5px] font-medium text-muted-foreground">
                A temporary password is generated on creation. The user must change it on first login.
              </span>

              <DialogFooter>
                <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
                  Cancel
                </Button>
                <Button type="submit" size="sm" disabled={isSubmitting}>
                  {isSubmitting ? 'Creating…' : 'Create User'}
                </Button>
              </DialogFooter>
            </form>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
