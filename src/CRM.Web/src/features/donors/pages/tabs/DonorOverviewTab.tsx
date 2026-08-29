import type { DonorDetailDto } from '../../types';
import { DetailSectionCard, DetailField } from '../../components/DetailSectionCard';
import { DonorStatusBadge } from '../../components/DonorStatusBadge';
import { formatDateTime, joinOrDash } from '../../lib/donorFormatters';

export function DonorOverviewTab({ donor }: { donor: DonorDetailDto }) {
  const { company, donations } = donor;
  const safeWebsite = (() => {
    if (!company.website) return null;
    try {
      const url = new URL(company.website);
      return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
    } catch {
      return null;
    }
  })();

  return (
    <div className="flex flex-col gap-4">
      <DetailSectionCard title="Company">
        <DetailField label="Company name" value={company.companyName} />
        <DetailField label="Company type" value={company.companyType.name} />
        <DetailField label="Registered name" value={company.registeredCompanyName} />
        <DetailField label="Trading name" value={company.tradingName} />
        <DetailField label="Entity type" value={company.entityType.name} />
        <DetailField label="Registration number" value={company.companyRegistrationNumber} />
        <DetailField label="Income tax number" value={company.incomeTaxNumber} />
        <DetailField
          label="Website"
          value={
            safeWebsite ? (
              <a href={safeWebsite} target="_blank" rel="noopener noreferrer" className="text-brand hover:underline">
                {company.website}
              </a>
            ) : undefined
          }
        />
      </DetailSectionCard>

      <DetailSectionCard title="Donations">
        <DetailField label="Frequency" value={donations.frequency.name} />
        <DetailField label="Donation types" value={joinOrDash(donations.types.map((t) => t.name))} />
        <DetailField label="Operational regions" value={joinOrDash(donations.operationalRegions.map((r) => r.name))} />
        <DetailField label="Collection address" value={donations.collectionAddress} fullWidth />
        <DetailField label="Operations / logistics details" value={donations.operationsLogisticsDetails} fullWidth />
      </DetailSectionCard>

      <DetailSectionCard title="Record">
        <DetailField label="Status" value={<DonorStatusBadge status={donor.status} />} />
        <DetailField label="Submission source" value={donor.submissionSource} />
        <DetailField label="FoodSpace company ID" value={donor.foodspaceCompanyId} />
        <DetailField label="Created" value={formatDateTime(donor.createdAt)} />
        <DetailField label="Last updated" value={formatDateTime(donor.updatedAt)} />
      </DetailSectionCard>
    </div>
  );
}
