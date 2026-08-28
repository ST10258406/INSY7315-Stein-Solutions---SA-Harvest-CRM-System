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
    <div className="flex flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-[#2B2B23] bg-[#141410]/60 px-6 py-10 text-center">
      <Icon className="h-5 w-5 text-[#6B6B60]" />
      <p className="text-sm font-medium text-[#B9B9AE]">{title}</p>
      <p className="text-xs text-[#6B6B60]">{description}</p>
    </div>
  );
}
