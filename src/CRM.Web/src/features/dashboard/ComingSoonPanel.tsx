import type { LucideIcon } from 'lucide-react';

interface ComingSoonPanelProps {
  icon: LucideIcon;
  title: string;
  description: string;
}

/**
 * The overdue follow-ups list and pending-approvals banner are present in
 * the Claude Design mockup but their APIs don't ship until Sprint 4. Render
 * this instead of wiring to nonexistent endpoints or hardcoding fake data.
 */
export function ComingSoonPanel({ icon: Icon, title, description }: ComingSoonPanelProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-border bg-card/60 px-6 py-14 text-center">
      <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-icon-bg">
        <Icon className="h-5 w-5 text-icon" />
      </span>
      <p className="m-0 text-sm font-bold tracking-tight text-foreground">{title}</p>
      <p className="m-0 max-w-[280px] text-xs font-medium text-muted-foreground">{description}</p>
    </div>
  );
}
