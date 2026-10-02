import { Filter, SlidersHorizontal } from 'lucide-react';
import {
  useCompanyTypes,
  useDonationFrequencies,
  useDonationTypes,
  useOperationalRegions,
  useRelationshipManagers,
} from '@/features/lookups';
import { DashboardFilterPopover } from './DashboardFilterPopover';
import type { DashboardScopeFilters, DashboardSegmentFilters } from './types';

function toId(value: string | undefined): number | undefined {
  return value ? Number(value) : undefined;
}

function fromId(value: number | undefined): string | undefined {
  return value === undefined ? undefined : String(value);
}

interface ScopeFilterProps {
  value: DashboardScopeFilters;
  onChange: (value: DashboardScopeFilters) => void;
}

/** Page-level "Filters" button: scopes every donor-backed widget to a manager and/or region. */
export function DashboardScopeFilter({ value, onChange }: ScopeFilterProps) {
  const managers = useRelationshipManagers();
  const regions = useOperationalRegions();

  return (
    <DashboardFilterPopover
      title="Dashboard filters"
      description="Applies to Donor activity and Overdue Follow-Ups."
      icon={Filter}
      triggerLabel="Filters"
      values={{ relationshipManagerId: value.relationshipManagerId, regionCode: value.regionCode }}
      onChange={(key, next) => onChange({ ...value, [key]: next })}
      onClear={() => onChange({})}
      fields={[
        {
          key: 'relationshipManagerId',
          label: 'Relationship manager',
          allLabel: 'All managers',
          isLoading: managers.isPending,
          options: (managers.data ?? []).map((m) => ({ value: m.id, label: m.fullName })),
        },
        {
          key: 'regionCode',
          label: 'Operational region',
          allLabel: 'All regions',
          isLoading: regions.isPending,
          options: (regions.data ?? []).map((r) => ({ value: r.code, label: r.name })),
        },
      ]}
    />
  );
}

interface SegmentFilterProps {
  value: DashboardSegmentFilters;
  onChange: (value: DashboardSegmentFilters) => void;
}

/** Donor activity section's own filter: narrows only its KPI cards by donor attributes. */
export function DonorActivityFilter({ value, onChange }: SegmentFilterProps) {
  const companyTypes = useCompanyTypes();
  const donationTypes = useDonationTypes();
  const frequencies = useDonationFrequencies();

  return (
    <DashboardFilterPopover
      title="Donor activity filters"
      description="Narrows the donor counts in this section."
      icon={SlidersHorizontal}
      values={{
        companyTypeId: fromId(value.companyTypeId),
        donationTypeId: fromId(value.donationTypeId),
        donationFrequencyId: fromId(value.donationFrequencyId),
      }}
      onChange={(key, next) => onChange({ ...value, [key]: toId(next) })}
      onClear={() => onChange({})}
      fields={[
        {
          key: 'companyTypeId',
          label: 'Company type',
          allLabel: 'All company types',
          isLoading: companyTypes.isPending,
          options: (companyTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name })),
        },
        {
          key: 'donationTypeId',
          label: 'Donation type',
          allLabel: 'All donation types',
          isLoading: donationTypes.isPending,
          options: (donationTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name })),
        },
        {
          key: 'donationFrequencyId',
          label: 'Donation frequency',
          allLabel: 'All frequencies',
          isLoading: frequencies.isPending,
          options: (frequencies.data ?? []).map((f) => ({ value: String(f.id), label: f.name })),
        },
      ]}
    />
  );
}
