import type { Control, FieldErrors, UseFormRegister } from 'react-hook-form';
import { useWatch } from 'react-hook-form';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import { TextField } from '../fields/TextField';

interface ContactsStepProps {
  register: UseFormRegister<DonorFormValues>;
  control: Control<DonorFormValues>;
  errors: FieldErrors<DonorFormValues>;
  onToggleMarketingContact: (checked: boolean) => void;
  onToggleAccountsContact: (checked: boolean) => void;
}

export function ContactsStep({ register, control, errors, onToggleMarketingContact, onToggleAccountsContact }: ContactsStepProps) {
  const hasMarketingContact = useWatch({ control, name: 'hasMarketingContact' });
  const hasAccountsContact = useWatch({ control, name: 'hasAccountsContact' });

  return (
    <div className="flex flex-col gap-6">
      <div>
        <p className="mb-3 text-[13px] font-extrabold text-[#16160F]">
          Primary Contact <span className="text-[#D4373A]">*</span>
        </p>
        <div className="grid grid-cols-1 gap-4.5 sm:grid-cols-2">
          <TextField label="Name" required registration={register('primaryContact.name')} error={errors.primaryContact?.name?.message} />
          <TextField label="Job Title" optional registration={register('primaryContact.jobTitle')} error={errors.primaryContact?.jobTitle?.message} />
          <TextField label="Phone" required registration={register('primaryContact.phone')} error={errors.primaryContact?.phone?.message} />
          <TextField label="Email" required type="email" placeholder="name@company.co.za" registration={register('primaryContact.email')} error={errors.primaryContact?.email?.message} />
        </div>
      </div>

      <div className="h-px bg-[#EDEDE8]" />

      <div>
        <label className="mb-3 flex items-center gap-2 text-[13px] font-extrabold text-[#16160F]">
          <input
            type="checkbox"
            checked={hasMarketingContact}
            onChange={(e) => onToggleMarketingContact(e.target.checked)}
            className="h-4 w-4 cursor-pointer accent-[#16160F]"
          />
          Marketing Contact <span className="font-medium text-[#9A9A90]">(optional)</span>
        </label>
        {hasMarketingContact && (
          <div className="grid grid-cols-1 gap-4.5 sm:grid-cols-3">
            <TextField label="Name" required registration={register('marketingContact.name')} error={errors.marketingContact?.name?.message} />
            <TextField label="Phone" required registration={register('marketingContact.phone')} error={errors.marketingContact?.phone?.message} />
            <TextField label="Email" required type="email" registration={register('marketingContact.email')} error={errors.marketingContact?.email?.message} />
          </div>
        )}
      </div>

      <div className="h-px bg-[#EDEDE8]" />

      <div>
        <label className="mb-1 flex items-center gap-2 text-[13px] font-extrabold text-[#16160F]">
          <input
            type="checkbox"
            checked={hasAccountsContact}
            onChange={(e) => onToggleAccountsContact(e.target.checked)}
            className="h-4 w-4 cursor-pointer accent-[#16160F]"
          />
          Accounts Contact <span className="font-medium text-[#9A9A90]">(optional)</span>
        </label>
        <p className="mb-3 text-[11.5px] font-medium text-[#82827A]">Used for Section 18A tax certificate correspondence</p>
        {hasAccountsContact && (
          <div className="grid grid-cols-1 gap-4.5 sm:grid-cols-3">
            <TextField label="Name" required registration={register('accountsContact.name')} error={errors.accountsContact?.name?.message} />
            <TextField label="Phone" required registration={register('accountsContact.phone')} error={errors.accountsContact?.phone?.message} />
            <TextField label="Email" required type="email" registration={register('accountsContact.email')} error={errors.accountsContact?.email?.message} />
          </div>
        )}
      </div>
    </div>
  );
}
