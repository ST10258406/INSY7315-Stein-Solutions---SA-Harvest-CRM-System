import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { useUpdateUser } from '../hooks';
import { applyServerErrors } from '../lib/applyServerErrors';
import { editUserFormSchema, type EditUserFormValues } from '../schemas';
import type { UserListItemDto } from '../types';

interface EditUserModalProps {
  user: UserListItemDto | null;
  onOpenChange: (open: boolean) => void;
}

export function EditUserModal({ user, onOpenChange }: EditUserModalProps) {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const updateUser = useUpdateUser();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<EditUserFormValues>({
    resolver: zodResolver(editUserFormSchema),
    // The parent remounts this component (key={user.id}) each time a different
    // user is selected, so these defaults are always fresh — no effect needed
    // to re-seed the form after the initial render.
    defaultValues: { firstName: user?.firstName ?? '', lastName: user?.lastName ?? '', email: user?.email ?? '' },
  });

  const handleOpenChange = (next: boolean) => {
    if (!next) onOpenChange(false);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (!user) return;
    setGeneralError(null);
    try {
      await updateUser.mutateAsync({ id: user.id, request: values });
      onOpenChange(false);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) {
        setGeneralError('Could not update this user. Please try again.');
      }
    }
  });

  return (
    <Dialog open={!!user} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Details</DialogTitle>
          <DialogDescription>Update {user ? `${user.firstName} ${user.lastName}` : 'this user'}'s account details.</DialogDescription>
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
              <label htmlFor="edit-user-first-name" className="text-xs font-bold text-foreground">
                First Name
              </label>
              <input
                id="edit-user-first-name"
                {...register('firstName')}
                className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 ${
                  errors.firstName ? 'border-destructive/70' : 'border-border'
                }`}
              />
              {errors.firstName && <span className="text-xs text-destructive">{errors.firstName.message}</span>}
            </div>

            <div className="flex flex-col gap-2">
              <label htmlFor="edit-user-last-name" className="text-xs font-bold text-foreground">
                Last Name
              </label>
              <input
                id="edit-user-last-name"
                {...register('lastName')}
                className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 ${
                  errors.lastName ? 'border-destructive/70' : 'border-border'
                }`}
              />
              {errors.lastName && <span className="text-xs text-destructive">{errors.lastName.message}</span>}
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="edit-user-email" className="text-xs font-bold text-foreground">
              Email
            </label>
            <input
              id="edit-user-email"
              type="email"
              {...register('email')}
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.email ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.email && <span className="text-xs text-destructive">{errors.email.message}</span>}
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
