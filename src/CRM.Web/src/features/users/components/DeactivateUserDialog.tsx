import { useState } from 'react';
import { AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { useSetUserActiveStatus } from '../hooks';
import type { UserListItemDto } from '../types';

interface DeactivateUserDialogProps {
  user: UserListItemDto | null;
  onOpenChange: (open: boolean) => void;
}

// Reactivating is a single click from the row menu (no confirmation needed —
// it only restores access). Deactivating is destructive to the user's ability
// to log in, so it goes through this confirm step.
export function DeactivateUserDialog({ user, onOpenChange }: DeactivateUserDialogProps) {
  const [error, setError] = useState<string | null>(null);
  const setActiveStatus = useSetUserActiveStatus();

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      setError(null);
      setActiveStatus.reset();
      onOpenChange(false);
    }
  };

  const confirm = async () => {
    if (!user) return;
    setError(null);
    try {
      await setActiveStatus.mutateAsync({ id: user.id, isActive: false });
      onOpenChange(false);
    } catch {
      setError('Could not deactivate this user. Please try again.');
    }
  };

  return (
    <Dialog open={!!user} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Deactivate {user ? `${user.firstName} ${user.lastName}` : 'user'}?</DialogTitle>
          <DialogDescription>
            They will no longer be able to log in, but their history and assigned records will be preserved.
          </DialogDescription>
        </DialogHeader>

        {error && (
          <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
            <AlertCircle className="h-3.75 w-3.75 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <DialogFooter>
          <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" variant="destructive" size="sm" onClick={confirm} disabled={setActiveStatus.isPending}>
            {setActiveStatus.isPending ? 'Deactivating…' : 'Deactivate'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
