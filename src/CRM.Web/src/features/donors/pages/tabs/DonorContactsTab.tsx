import { Users, Phone, Mail, FileText, Plus, type LucideIcon } from 'lucide-react';
import type { DonorContactDto, DonorDetailDto } from '../../types';
import { Button } from '@/components/ui/button';

function ContactCard({
  title,
  contact,
  icon: Icon,
  isActive = true
}: {
  title: string;
  contact: DonorContactDto | null;
  icon: LucideIcon;
  isActive?: boolean;
}) {
  if (!contact) {
    return (
      <div className="bg-[var(--card)] border border-[var(--border)] rounded-2xl shadow-[0_1px_3px_var(--shadow)] p-5.5">
        <div className="flex items-center justify-between mb-4.5">
          <h2 className="m-0 text-xs font-extrabold tracking-wider text-[var(--muted-c)]">
            {title}
          </h2>
          <span className="w-6.5 h-6.5 rounded-full bg-[var(--icon-bg)] flex items-center justify-center">
            <Icon className="w-3.5 h-3.5 text-[var(--muted2)]" />
          </span>
        </div>
        <div className="flex flex-col items-start gap-2 py-4 pb-5">
          <div className="text-sm font-semibold text-[var(--muted2)]">Not provided.</div>
          <p className="m-0 text-[12.5px] font-medium text-[var(--muted2)] leading-relaxed">
            This donor did not supply {title.toLowerCase()} details.
          </p>
          <Button variant="outline" size="sm" className="mt-1.5 h-8.5 rounded-full border-dashed">
            <Plus className="w-3.25 h-3.25 stroke-[2.2]" />
            <span>Add contact</span>
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-[var(--card)] border border-[var(--border)] rounded-2xl shadow-[0_1px_3px_var(--shadow)] p-5.5">
      <div className="flex items-center justify-between mb-4.5">
        <h2 className={`m-0 text-xs font-extrabold tracking-wider ${isActive ? 'text-[var(--ink)]' : 'text-[var(--muted-c)]'}`}>
          {title}
        </h2>
        <span className="w-6.5 h-6.5 rounded-full bg-[var(--icon-bg)] flex items-center justify-center">
          <Icon className={`w-3.5 h-3.5 ${isActive ? 'text-[var(--icon)]' : 'text-[var(--muted2)]'}`} />
        </span>
      </div>
      <div className="text-base font-extrabold tracking-tight text-[var(--ink)]">
        {contact.name}
      </div>
      {contact.jobTitle && (
        <div className="text-[12.5px] font-semibold text-[var(--muted-c)] mt-1">
          {contact.jobTitle}
        </div>
      )}
      <div className="flex flex-col gap-2.5 mt-4.5">
        {contact.phone && (
          <a
            href={`tel:${contact.phone}`}
            className="flex items-center gap-2.5 text-[13.5px] font-semibold text-[var(--ink)] hover:text-brand"
          >
            <span className="w-7.5 h-7.5 rounded-full bg-[var(--field)] border border-[var(--border)] flex items-center justify-center shrink-0">
              <Phone className="w-3.5 h-3.5 text-[var(--icon)]" />
            </span>
            <span>{contact.phone}</span>
          </a>
        )}
        {contact.email && (
          <a
            href={`mailto:${contact.email}`}
            className="flex items-center gap-2.5 text-[13.5px] font-semibold text-[var(--ink)] hover:text-brand min-w-0"
          >
            <span className="w-7.5 h-7.5 rounded-full bg-[var(--field)] border border-[var(--border)] flex items-center justify-center shrink-0">
              <Mail className="w-3.5 h-3.5 text-[var(--icon)]" />
            </span>
            <span className="truncate">{contact.email}</span>
          </a>
        )}
      </div>
    </div>
  );
}

export function DonorContactsTab({ donor }: { donor: DonorDetailDto }) {
  return (
    <section className="grid grid-cols-1 md:grid-cols-3 gap-4.5">
      <ContactCard 
        title="PRIMARY CONTACT" 
        contact={donor.primaryContact} 
        icon={Users} 
      />
      <ContactCard 
        title="MARKETING CONTACT" 
        contact={donor.marketingContact} 
        icon={FileText} 
      />
      <ContactCard 
        title="ACCOUNTS CONTACT" 
        contact={donor.accountsContact} 
        icon={FileText} 
        isActive={!!donor.accountsContact}
      />
    </section>
  );
}
