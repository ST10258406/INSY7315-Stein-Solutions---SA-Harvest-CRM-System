import { useState } from 'react';
import { CheckCircle2, Plus, TriangleAlert, Undo2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAddReviewerFlag } from '../hooks';
import { VettingDecisionDialog } from './VettingDecisionDialog';
import type { VettingDecision, VettingSubmission } from '../types';

/** Approve / Return to officer / Decline. Render only for Admins on a Ready submission. */
export function DecisionButtons({ submission }: { submission: VettingSubmission }) {
  const [decision, setDecision] = useState<VettingDecision | null>(null);
  return (
    <>
      <Button size="sm" variant="approve" onClick={() => setDecision('Approve')}>
        <CheckCircle2 className="h-3.5 w-3.5" /> Approve
      </Button>
      <Button size="sm" variant="outline" onClick={() => setDecision('Return')}>
        <Undo2 className="h-3.5 w-3.5" /> Return to officer
      </Button>
      <Button size="sm" variant="reject" onClick={() => setDecision('Decline')}>
        <TriangleAlert className="h-3.5 w-3.5" /> Decline
      </Button>
      <VettingDecisionDialog submission={submission} decision={decision} onOpenChange={(open) => !open && setDecision(null)} />
    </>
  );
}

const FLAG_MAX = 255;

/** Inline "Add flag" for the Flags panel. Render only for Admins on a Ready submission. */
export function AddFlagForm({ submissionId }: { submissionId: string }) {
  const [description, setDescription] = useState('');
  const addFlag = useAddReviewerFlag(submissionId);
  const trimmed = description.trim();

  return (
    <form
      className="mt-3 flex flex-col gap-1.5"
      onSubmit={(event) => {
        event.preventDefault();
        if (!trimmed) return;
        addFlag.mutate(trimmed, { onSuccess: () => setDescription('') });
      }}
    >
      <div className="flex gap-2">
        <input
          aria-label="New reviewer flag"
          value={description}
          maxLength={FLAG_MAX}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Add a flag for the record, e.g. Fridge not working"
          className="h-10 min-w-0 flex-1 rounded-full border border-border bg-field px-3.75 text-[13px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60"
        />
        <Button type="submit" size="sm" variant="outline" disabled={!trimmed || addFlag.isPending}>
          <Plus className="h-3.5 w-3.5" /> Add flag
        </Button>
      </div>
      {addFlag.error && (
        <span className="text-xs text-destructive">{addFlag.error.response?.data?.message ?? 'Could not add this flag.'}</span>
      )}
    </form>
  );
}
