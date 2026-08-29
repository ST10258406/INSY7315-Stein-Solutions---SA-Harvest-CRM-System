import type { DonorDetailDto } from '../../types';
import { DetailSectionCard, DetailField } from '../../components/DetailSectionCard';
import { formatDate, isOverdue } from '../../lib/donorFormatters';

export function DonorCrmTab({ donor }: { donor: DonorDetailDto }) {
  const { crm } = donor;

  return (
    <div className="flex flex-col gap-4">
      <DetailSectionCard title="Relationship">
        <DetailField label="Relationship manager" value={crm.relationshipManager?.fullName ?? 'Unassigned'} />
        <DetailField
          label="Follow-up date"
          value={
            <span className={isOverdue(crm.followUpDate) ? 'font-medium text-rose-400' : undefined}>
              {formatDate(crm.followUpDate)}
            </span>
          }
        />
      </DetailSectionCard>

      <DetailSectionCard title="Marketing">
        <DetailField label="Marketing consent" value={crm.marketingConsent ? 'Given' : 'Not given'} />
        <DetailField label="Consent date" value={formatDate(crm.marketingConsentDate)} />
        <DetailField
          label="Impact reporting preferences"
          value={crm.impactReportingPreferences}
          fullWidth
        />
      </DetailSectionCard>

      <DetailSectionCard title="Notes">
        <DetailField label="Additional information" value={crm.additionalInformation} fullWidth />
      </DetailSectionCard>
    </div>
  );
}
