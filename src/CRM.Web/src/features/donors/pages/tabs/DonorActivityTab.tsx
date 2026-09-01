import { ListChecks } from 'lucide-react';
import { ComingSoonPanel } from '@/features/dashboard/ComingSoonPanel';
import { InteractionFeed } from '@/features/interactions';
import type { DonorDetailDto } from '../../types';

/**
 * Activity tab: the interaction timeline (Issue #107) on the left, with the
 * Tasks column (Issue #108) still pending its endpoints — same placeholder
 * rule as the Dashboard ticket. Layout mirrors the Claude design's two-column
 * Activity view so Tasks drops straight in once #108 ships.
 */
export function DonorActivityTab({ donor }: { donor: DonorDetailDto }) {
  return (
    <section className="grid grid-cols-1 items-start gap-4.5 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]">
      <InteractionFeed donorId={donor.id} />
      <div>
        <h2 className="mb-3.5 text-[15px] font-extrabold tracking-tight text-[var(--ink)]">Tasks</h2>
        <ComingSoonPanel
          icon={ListChecks}
          title="Tasks coming soon"
          description="Donor tasks and reminders will appear here once the Tasks API ships."
        />
      </div>
    </section>
  );
}
