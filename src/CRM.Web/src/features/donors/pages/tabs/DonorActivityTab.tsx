import { History } from 'lucide-react';
import { ComingSoonPanel } from '@/features/dashboard/ComingSoonPanel';

/**
 * Interaction timeline + tasks depend on the Interactions/Tasks endpoints,
 * which don't ship until Sprint 4 — same placeholder rule as the Dashboard
 * ticket (Issue 39). Shown explicitly rather than hiding the tab, per design.
 */
export function DonorActivityTab() {
  return (
    <ComingSoonPanel
      icon={History}
      title="Interaction history coming soon"
      description="Timeline of calls, visits, and tasks will appear here once the Interactions API ships."
    />
  );
}
