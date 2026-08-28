import { useState } from 'react';
import { Search, Bookmark, CalendarDays, Bot, HelpCircle, ChevronLeft, ChevronRight } from 'lucide-react';

/**
 * Slim icon-only rail for secondary quick actions, per the Claude Design
 * hybrid-nav spec (deviation from the original SDD sidebar). Primary
 * navigation lives in TopBar — this rail is deliberately non-navigational.
 */
export function Sidebar() {
  const [isOpen, setIsOpen] = useState(true);

  const quickActions = [
    { title: 'Search', icon: Search },
    { title: 'Saved filters', icon: Bookmark },
    { title: 'Calendar', icon: CalendarDays },
    { title: 'Agent', icon: Bot },
    { title: 'Help & support', icon: HelpCircle },
  ];

  if (!isOpen) {
    return (
      <aside className="flex w-[30px] shrink-0 items-end justify-center bg-background pb-4">
        <button
          type="button"
          onClick={() => setIsOpen(true)}
          title="Expand rail"
          className="flex h-10 w-6 items-center justify-center rounded-lg border border-border bg-card text-icon transition-colors hover:border-foreground"
        >
          <ChevronRight className="h-3.5 w-3.5" />
        </button>
      </aside>
    );
  }

  return (
    <aside className="flex w-[68px] shrink-0 flex-col items-center bg-background py-5.5 pb-4">
      <div className="mt-4 flex flex-col gap-1 rounded-[26px] border border-border bg-card p-1.5 shadow-[0_1px_3px_var(--shadow)]">
        {quickActions.map(({ title, icon: Icon }) => (
          <button
            key={title}
            type="button"
            title={title}
            className="flex h-9.5 w-9.5 items-center justify-center rounded-full text-icon transition-colors hover:bg-hover"
          >
            <Icon className="h-4 w-4" />
          </button>
        ))}
      </div>

      <button
        type="button"
        onClick={() => setIsOpen(false)}
        title="Collapse rail"
        className="mt-auto flex h-10 w-10 items-center justify-center rounded-full border border-border bg-card text-icon transition-colors hover:border-foreground"
      >
        <ChevronLeft className="h-4 w-4" />
      </button>
    </aside>
  );
}
