import type { DonorContactDto, DonorDetailDto } from '../../types';
import { DetailSectionCard, DetailField } from '../../components/DetailSectionCard';

function ContactCard({ title, contact }: { title: string; contact: DonorContactDto | null }) {
  if (!contact) {
    return (
      <div className="rounded-2xl border border-dashed border-[#2B2B23] bg-[#141410]/60 p-5">
        <h3 className="text-xs font-semibold tracking-wide text-[#B9B9AE] uppercase">{title}</h3>
        <p className="mt-4 text-sm text-[#6B6B60]">Not provided.</p>
      </div>
    );
  }

  return (
    <DetailSectionCard title={title}>
      <DetailField label="Name" value={contact.name} fullWidth />
      <DetailField label="Job title" value={contact.jobTitle} />
      <DetailField
        label="Phone"
        value={
          contact.phone ? (
            <a href={`tel:${contact.phone}`} className="text-brand hover:underline">
              {contact.phone}
            </a>
          ) : undefined
        }
      />
      <DetailField
        label="Email"
        value={
          contact.email ? (
            <a href={`mailto:${contact.email}`} className="text-brand hover:underline">
              {contact.email}
            </a>
          ) : undefined
        }
        fullWidth
      />
    </DetailSectionCard>
  );
}

export function DonorContactsTab({ donor }: { donor: DonorDetailDto }) {
  return (
    <div className="flex flex-col gap-4">
      <ContactCard title="Primary contact" contact={donor.primaryContact} />
      <ContactCard title="Marketing contact" contact={donor.marketingContact} />
      <ContactCard title="Accounts contact" contact={donor.accountsContact} />
    </div>
  );
}
