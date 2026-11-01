import { useImperativeHandle, useState, type ReactNode, type Ref } from 'react';
import { Accordion } from '@/components/ui/accordion';
import { Button } from '@/components/ui/button';
import { AccordionOpenContext } from './accordionOpenContext';

export interface AccordionFormLayoutHandle {
  /** Opens these sections (keeping any already open), e.g. every section with errors after a failed submit. */
  openSections: (ids: string[]) => void;
}

interface AccordionFormLayoutProps {
  /** Every section id, in order — what Expand all opens. */
  sectionIds: string[];
  defaultOpen?: string[];
  ref?: Ref<AccordionFormLayoutHandle>;
  children: ReactNode;
}

/** Owns which FormAccordionSections are open. Any number can be open at once. */
export function AccordionFormLayout({ sectionIds, defaultOpen = [], ref, children }: AccordionFormLayoutProps) {
  const [open, setOpen] = useState<string[]>(defaultOpen);

  useImperativeHandle(ref, () => ({
    openSections: (ids) => setOpen((current) => [...new Set([...current, ...ids])]),
  }), []);

  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" size="sm" onClick={() => setOpen(sectionIds)}>
          Expand all
        </Button>
        <Button type="button" variant="outline" size="sm" onClick={() => setOpen([])}>
          Collapse all
        </Button>
      </div>
      <Accordion multiple value={open} onValueChange={(value) => setOpen(value as string[])} className="gap-3">
        <AccordionOpenContext.Provider value={open}>{children}</AccordionOpenContext.Provider>
      </Accordion>
    </div>
  );
}
