import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ChevronDown } from 'lucide-react';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { useAuthStore } from '@/store/authStore';
import { HELP_SHORTCUTS, HELP_TOPICS, buildProblemReportBody, canSee } from '../content';

interface HelpDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function HelpDrawer({ open, onOpenChange }: HelpDrawerProps) {
  const roles = useAuthStore((state) => state.user?.roles) ?? [];
  const { pathname } = useLocation();
  const [query, setQuery] = useState('');
  const [expanded, setExpanded] = useState<string | null>(null);

  const supportEmail = import.meta.env.VITE_SUPPORT_EMAIL as string | undefined;
  const version = (import.meta.env.VITE_APP_VERSION as string | undefined) || 'dev';

  const term = query.trim().toLowerCase();
  const topics = HELP_TOPICS.filter((t) => canSee(t, roles)).filter(
    (t) => !term || [t.title, ...t.steps].some((text) => text.toLowerCase().includes(term)),
  );
  const shortcuts = HELP_SHORTCUTS.filter((s) => canSee(s, roles));

  const reportHref = supportEmail
    ? `mailto:${supportEmail}?subject=${encodeURIComponent('CRM problem report')}&body=${encodeURIComponent(
        buildProblemReportBody(pathname, version, navigator.userAgent, roles[0] ?? 'Unknown'),
      )}`
    : undefined;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[440px]">
        <SheetHeader>
          <SheetTitle>Help &amp; support</SheetTitle>
          <SheetDescription>Short how-tos for the parts of the CRM you can use.</SheetDescription>
        </SheetHeader>

        <div className="min-h-0 flex-1 overflow-y-auto px-6.5 py-4">
          <input
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search help"
            aria-label="Search help topics"
            className="w-full rounded-full border border-border bg-field px-4 py-2 text-[13px] text-foreground outline-none focus:border-foreground"
          />

          <ul className="m-0 mt-4 flex list-none flex-col gap-2 p-0">
            {topics.map((topic) => {
              const isOpen = expanded === topic.id;
              const panelId = `help-topic-${topic.id}`;
              return (
                <li key={topic.id} className="rounded-2xl border border-border bg-card">
                  <button
                    type="button"
                    aria-expanded={isOpen}
                    aria-controls={panelId}
                    onClick={() => setExpanded(isOpen ? null : topic.id)}
                    className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left text-[13px] font-semibold text-foreground"
                  >
                    {topic.title}
                    <ChevronDown className={`h-4 w-4 shrink-0 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
                  </button>
                  {isOpen && (
                    <div id={panelId} className="px-4 pb-3.5">
                      <ol className="m-0 flex flex-col gap-1.5 pl-5 text-[12.5px] text-muted-foreground">
                        {topic.steps.map((step) => (
                          <li key={step}>{step}</li>
                        ))}
                      </ol>
                      {topic.path && (
                        <Link
                          to={topic.path}
                          onClick={() => onOpenChange(false)}
                          className="mt-2.5 inline-block text-[12.5px] font-bold text-foreground underline"
                        >
                          Go there
                        </Link>
                      )}
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
          {topics.length === 0 && (
            <p className="mt-4 text-[12.5px] text-muted-foreground">No help topics match “{query}”.</p>
          )}

          <h3 className="mb-2 mt-6 text-[12.5px] font-bold text-foreground">Keyboard shortcuts</h3>
          <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1.5 text-[12.5px]">
            {shortcuts.map((s) => (
              <div key={s.keys} className="contents">
                <dt>
                  <kbd className="rounded border border-border bg-field px-1.5 py-0.5 font-mono text-[11.5px]">{s.keys}</kbd>
                </dt>
                <dd className="m-0 text-muted-foreground">{s.description}</dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="flex flex-shrink-0 flex-col gap-1.5 border-t border-[var(--divider)] px-6.5 py-4 text-[12.5px]">
          {supportEmail && (
            <>
              <a href={`mailto:${supportEmail}`} className="font-bold text-foreground underline">
                Contact support
              </a>
              <a href={reportHref} className="font-bold text-foreground underline">
                Report a problem
              </a>
            </>
          )}
          <span className="text-muted-foreground">Version {version}</span>
        </div>
      </SheetContent>
    </Sheet>
  );
}
