import type { ReactNode } from 'react';
import { Tabs, TabsList, TabsTab, TabsPanel } from '@/components/ui/tabs';
import type { DonorDetailDto } from '../types';
import { DonorOverviewTab } from './tabs/DonorOverviewTab';
import { DonorContactsTab } from './tabs/DonorContactsTab';
import { DonorLegalTab } from './tabs/DonorLegalTab';
import { DonorCrmTab } from './tabs/DonorCrmTab';
import { DonorActivityTab } from './tabs/DonorActivityTab';

const TABS = [
  { value: 'overview', label: 'Overview' },
  { value: 'contacts', label: 'Contacts' },
  { value: 'legal', label: 'Legal' },
  { value: 'crm', label: 'CRM' },
  { value: 'activity', label: 'Activity' },
] as const;

const TAB_LIST_CLASSES = 'flex items-center gap-1 mt-5 overflow-x-auto no-scrollbar w-full flex-nowrap justify-start rounded-none border-none bg-transparent p-0';

interface DonorTabsProps {
  donor: DonorDetailDto;
  children: ReactNode;
}

/** Owns the whole tabbed detail layout: the soft header section (title/badges,
 * passed in as `children`) plus the tab bar sit together, while the tab
 * panels render below it on the page background — matching the reference
 * design's two-tier layout. */
export function DonorTabs({ donor, children }: DonorTabsProps) {
  return (
    <Tabs defaultValue="overview">
      <section className="rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-[20px_22px_0]">
        {children}
        <TabsList className={TAB_LIST_CLASSES}>
          {TABS.map((tab) => {
            const badgeCount = tab.value === 'contacts' ? '2' : null;
              
            return (
              <TabsTab 
                key={tab.value} 
                value={tab.value} 
                className="group flex items-center gap-2 h-9 rounded-full border border-transparent px-4.5 text-[13.5px] font-semibold text-[var(--muted-c)] cursor-pointer whitespace-nowrap transition-all hover:text-[var(--ink)] data-[selected]:border-[var(--border)] data-[selected]:bg-[var(--card)] data-[selected]:shadow-sm data-[selected]:text-[var(--ink)]"
              >
                <span>{tab.label}</span>
                {badgeCount && (
                  <span
                    className="min-w-[20px] h-[20px] px-1.5 rounded-full inline-flex items-center justify-center text-[10.5px] font-bold bg-[var(--card)] text-[var(--muted-c)] group-data-[selected]:bg-[var(--ink)]/10 group-data-[selected]:text-[var(--ink)]"
                  >
                    {badgeCount}
                  </span>
                )}
              </TabsTab>
            );
          })}
        </TabsList>
      </section>

      <div className="mt-5">
        <TabsPanel value="overview">
          <DonorOverviewTab donor={donor} />
        </TabsPanel>
        <TabsPanel value="contacts">
          <DonorContactsTab donor={donor} />
        </TabsPanel>
        <TabsPanel value="legal">
          <DonorLegalTab donor={donor} />
        </TabsPanel>
        <TabsPanel value="crm">
          <DonorCrmTab donor={donor} />
        </TabsPanel>
        <TabsPanel value="activity">
          <DonorActivityTab donor={donor} />
        </TabsPanel>
      </div>
    </Tabs>
  );
}
