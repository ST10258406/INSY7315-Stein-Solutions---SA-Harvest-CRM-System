import { InteractionFeed } from '@/features/interactions';
import { DonorTasksPanel } from '@/features/tasks';
import type { DonorDetailDto } from '../../types';

/**
 * Activity tab: the interaction timeline (Issue #107) alongside the donor's
 * task list (Issue #108), matching the Claude design's two-column Activity view.
 */
export function DonorActivityTab({ donor }: { donor: DonorDetailDto }) {
  return (
    <section className="grid grid-cols-1 items-start gap-4.5 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]">
      <InteractionFeed donorId={donor.id} />
      <DonorTasksPanel donorId={donor.id} donorName={donor.company.companyName} />
    </section>
  );
}
