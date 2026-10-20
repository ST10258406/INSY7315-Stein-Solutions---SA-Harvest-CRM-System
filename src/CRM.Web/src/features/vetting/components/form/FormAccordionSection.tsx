import type { ReactNode } from 'react';
import { ChevronDown, type LucideIcon } from 'lucide-react';
import { AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';

interface FormAccordionSectionProps {
  /** Accordion item value; also what AccordionFormLayout.openSections() takes. */
  id: string;
  number: number;
  title: string;
  description?: string;
  icon?: LucideIcon;
  /** Shown on the header even while collapsed, so errors in a closed section are never invisible. */
  errorCount?: number;
  children: ReactNode;
}

/** The collapsible equivalent of donors' FormSection: same header and 2-column card body. */
export function FormAccordionSection({ id, number, title, description, icon: Icon, errorCount = 0, children }: FormAccordionSectionProps) {
  return (
    <AccordionItem value={id} id={id} className="rounded-2xl border border-border bg-muted/40 not-last:border-b">
      <AccordionTrigger className="items-center gap-2.75 rounded-2xl px-5 py-4 hover:no-underline **:data-[slot=accordion-trigger-icon]:hidden">
        <span className="flex h-6 min-w-6 shrink-0 items-center justify-center rounded-full bg-secondary px-1.5 text-xs font-bold text-foreground">
          {number}
        </span>
        {Icon && (
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground">
            <Icon className="h-4 w-4" />
          </span>
        )}
        <span className="min-w-0 flex-1">
          <span className="block text-base font-bold tracking-tight text-foreground">{title}</span>
          {description && <span className="mt-0.5 block text-xs font-medium text-muted-foreground">{description}</span>}
        </span>
        {errorCount > 0 && (
          <span className="shrink-0 rounded-full bg-destructive/10 px-2.5 py-0.5 text-xs font-bold text-destructive">
            {errorCount} {errorCount === 1 ? 'error' : 'errors'}
          </span>
        )}
        <ChevronDown
          aria-hidden
          className="h-4 w-4 shrink-0 text-muted-foreground transition-transform group-aria-expanded/accordion-trigger:rotate-180"
        />
      </AccordionTrigger>
      {/* keepMounted: collapsed fields stay in the DOM, so react-hook-form keeps their refs and values. */}
      <AccordionContent keepMounted className="px-5 pb-5">
        <div className="grid grid-cols-1 gap-x-6 gap-y-4 rounded-xl border border-border bg-card p-5 text-sm shadow-sm sm:grid-cols-2">
          {children}
        </div>
      </AccordionContent>
    </AccordionItem>
  );
}
