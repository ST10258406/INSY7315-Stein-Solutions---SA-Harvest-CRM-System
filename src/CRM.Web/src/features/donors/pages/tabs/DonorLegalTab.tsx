import type { DonorDetailDto } from '../../types';
import { DetailSectionCard, DetailField } from '../../components/DetailSectionCard';
import { DonorDocumentsSection } from '../../components/DonorDocumentsSection';

export function DonorLegalTab({ donor }: { donor: DonorDetailDto }) {
  const { legalAddress, compliance } = donor;

  return (
    <div className="flex flex-col gap-4">
      {legalAddress ? (
        <DetailSectionCard title="Registered address">
          <DetailField label="Street address" value={legalAddress.streetNameNumber} fullWidth />
          <DetailField label="Suburb" value={legalAddress.suburb} />
          <DetailField label="City" value={legalAddress.city} />
          <DetailField label="Province" value={legalAddress.province.name} />
          <DetailField label="Postal code" value={legalAddress.postalCode} />
        </DetailSectionCard>
      ) : (
        <div className="rounded-2xl border border-dashed border-border bg-card/60 p-5">
          <h3 className="text-xs font-semibold tracking-wide text-muted-foreground uppercase">Registered address</h3>
          <p className="mt-4 text-sm text-muted-foreground">No legal address on file.</p>
        </div>
      )}

      <DetailSectionCard title="Compliance">
        <DetailField label="B-BBEE status" value={compliance.bbbeeStatus?.name ?? 'Not verified'} />
      </DetailSectionCard>

      <DonorDocumentsSection documents={compliance.documents} />
    </div>
  );
}
