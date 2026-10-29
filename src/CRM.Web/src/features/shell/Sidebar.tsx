import { useEffect, useState } from 'react';
import { Search, Bookmark, CalendarDays, Bot, HelpCircle, ChevronLeft, ChevronRight } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { AgentDrawer } from '@/features/agent';
import { HelpDrawer } from '@/features/help';
import { QuickSearchDialog } from '@/features/search';
import { CalendarDialog } from '@/features/calendar';
import { SavedFiltersMenu } from '@/features/savedFilters';
import { PROCUREMENT_OR_ABOVE } from './navItems';

const ACTION_BUTTON_CLASS =
  'flex h-9.5 w-9.5 items-center justify-center rounded-full text-icon transition-colors hover:bg-hover';

/**
 * Slim icon-only rail for secondary quick actions, per the Claude Design
 * hybrid-nav spec (deviation from the original SDD sidebar). Primary
 * navigation lives in TopBar — this rail is deliberately non-navigational.
 *
 * The Agent action (CRM assistant) is SuperAdmin-only, so it renders for
 * SuperAdmins only. Cosmetic — the API enforces access.
 */
export function Sidebar() {
  const [isOpen, setIsOpen] = useState(true);
  const [isAgentOpen, setIsAgentOpen] = useState(false);
  const [isHelpOpen, setIsHelpOpen] = useState(false);
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [isCalendarOpen, setIsCalendarOpen] = useState(false);
  const isSuperAdmin = useAuthStore((state) => state.user?.roles.includes('SuperAdmin') ?? false);
  const canUseDonorList = useAuthStore((state) => state.user?.roles.some((r) => PROCUREMENT_OR_ABOVE.includes(r)) ?? false);

  // Ctrl+K / Cmd+K opens quick search from anywhere (Issue 102). preventDefault stops the
  // browser's own Ctrl+K (focus the address bar search).
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k' && !e.repeat) {
        e.preventDefault();
        setIsSearchOpen(true);
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, []);

  // Rendered in both rail states, so Ctrl+K still works while the rail is collapsed.
  const overlays = (
    <>
      <QuickSearchDialog open={isSearchOpen} onOpenChange={setIsSearchOpen} />
      <CalendarDialog open={isCalendarOpen} onOpenChange={setIsCalendarOpen} />
      {isSuperAdmin && <AgentDrawer open={isAgentOpen} onOpenChange={setIsAgentOpen} />}
      <HelpDrawer open={isHelpOpen} onOpenChange={setIsHelpOpen} />
    </>
  );

  const quickActions = [
    { title: 'Search', icon: Search, ariaLabel: 'Open quick search (Ctrl+K)', onClick: () => setIsSearchOpen(true) },
    ...(canUseDonorList ? [{ title: 'Saved filters', icon: Bookmark, isMenu: true }] : []),
    { title: 'Calendar', icon: CalendarDays, ariaLabel: 'Open calendar', onClick: () => setIsCalendarOpen(true) },
    ...(isSuperAdmin
      ? [{ title: 'Agent', icon: Bot, ariaLabel: 'Open CRM assistant', onClick: () => setIsAgentOpen(true) }]
      : []),
    { title: 'Help & support', icon: HelpCircle, ariaLabel: 'Open help and support', onClick: () => setIsHelpOpen(true) },
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
        {overlays}
      </aside>
    );
  }

  return (
    <aside className="flex w-[68px] shrink-0 flex-col items-center bg-background py-5.5 pb-4">
      <div className="mt-4 flex flex-col gap-1 rounded-[26px] border border-border bg-card p-1.5 shadow-[0_1px_3px_var(--shadow)]">
        {quickActions.map(({ title, icon: Icon, ariaLabel, onClick, isMenu }) =>
          isMenu ? (
            <SavedFiltersMenu key={title} triggerClassName={ACTION_BUTTON_CLASS} />
          ) : (
            <button
              key={title}
              type="button"
              title={title}
              aria-label={ariaLabel ?? title}
              onClick={onClick}
              className={ACTION_BUTTON_CLASS}
            >
              <Icon className="h-4 w-4" />
            </button>
          ),
        )}
      </div>

      <button
        type="button"
        onClick={() => setIsOpen(false)}
        title="Collapse rail"
        className="mt-auto flex h-10 w-10 items-center justify-center rounded-full border border-border bg-card text-icon transition-colors hover:border-foreground"
      >
        <ChevronLeft className="h-4 w-4" />
      </button>

      {overlays}
    </aside>
  );
}
