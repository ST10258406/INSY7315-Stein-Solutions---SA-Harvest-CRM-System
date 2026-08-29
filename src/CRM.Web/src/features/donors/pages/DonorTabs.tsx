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

export function DonorTabs({ donor }: { donor: DonorDetailDto }) {
  return (
    <Tabs defaultValue="overview">
      <TabsList>
        {TABS.map((tab) => (
          <TabsTab key={tab.value} value={tab.value}>
            {tab.label}
          </TabsTab>
        ))}
      </TabsList>

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
        <DonorActivityTab />
      </TabsPanel>
    </Tabs>
  );
}
