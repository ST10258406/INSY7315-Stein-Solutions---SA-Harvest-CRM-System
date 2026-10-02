import type { Control } from 'react-hook-form';
import { useWatch } from 'react-hook-form';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import type { LookupDto, ProvinceDto } from '@/features/donors/types';
import {
  usePublicBbbeeStatuses,
  usePublicCompanyTypes,
  usePublicDonationFrequencies,
  usePublicDonationTypes,
  usePublicEntityTypes,
  usePublicOperationalRegions,
  usePublicProvinces,
} from '../../hooks/usePublicLookups';

interface ReviewStepProps {
  control: Control<DonorFormValues>;
  hasSignature: boolean;
  confirmChecked: boolean;
  onConfirmChange: (checked: boolean) => void;
  confirmError?: string;
  onEditStep: (step: number) => void;
}

function lookupLabel(items: LookupDto[] | ProvinceDto[] | undefined, id: string): string {
  if (!id) return '—';
  return items?.find((item) => String(item.id) === id)?.name ?? '—';
}

function lookupLabels(items: LookupDto[] | undefined, ids: string[]): string {
  if (ids.length === 0) return '—';
  const idSet = new Set(ids);
  const names = (items ?? []).filter((item) => idSet.has(String(item.id))).map((item) => item.name);
  return names.length > 0 ? names.join(', ') : '—';
}

function orDash(value: string | undefined): string {
  return value && value.trim() ? value : '—';
}

interface ReviewGroup {
  title: string;
  step: number;
  rows: { label: string; value: string }[];
}

export function ReviewStep({ control, hasSignature, confirmChecked, onConfirmChange, confirmError, onEditStep }: ReviewStepProps) {
  const values = useWatch({ control });

  const companyTypes = usePublicCompanyTypes();
  const entityTypes = usePublicEntityTypes();
  const provinces = usePublicProvinces();
  const donationFrequencies = usePublicDonationFrequencies();
  const donationTypes = usePublicDonationTypes();
  const operationalRegions = usePublicOperationalRegions();
  const bbbeeStatuses = usePublicBbbeeStatuses();

  const company = values.company;
  const primaryContact = values.primaryContact;
  const legalAddress = values.legalAddress;
  const donations = values.donations;

  const groups: ReviewGroup[] = [
    {
      title: 'Company Information',
      step: 1,
      rows: [
        { label: 'Company Name', value: orDash(company?.companyName) },
        { label: 'Company Type', value: lookupLabel(companyTypes.data, company?.companyTypeId ?? '') },
        { label: 'Registered Name', value: orDash(company?.registeredCompanyName) },
        { label: 'Trading Name', value: orDash(company?.tradingName) },
        { label: 'Legal Entity Type', value: lookupLabel(entityTypes.data, company?.entityTypeId ?? '') },
        { label: 'Registration Number', value: orDash(company?.companyRegistrationNumber) },
        { label: 'Income Tax Number', value: orDash(company?.incomeTaxNumber) },
        { label: 'Website', value: orDash(company?.website) },
      ],
    },
    {
      title: 'Contacts',
      step: 2,
      rows: [
        { label: 'Primary Contact', value: orDash(primaryContact?.name) },
        { label: 'Primary Phone / Email', value: `${orDash(primaryContact?.phone)} · ${orDash(primaryContact?.email)}` },
        { label: 'Marketing Contact', value: values.hasMarketingContact ? orDash(values.marketingContact?.name) : '—' },
        { label: 'Accounts Contact', value: values.hasAccountsContact ? orDash(values.accountsContact?.name) : '—' },
      ],
    },
    {
      title: 'Registered Address',
      step: 3,
      rows: [
        { label: 'Street', value: orDash(legalAddress?.streetAddress) },
        { label: 'Suburb', value: orDash(legalAddress?.suburb) },
        { label: 'City', value: orDash(legalAddress?.city) },
        { label: 'Province', value: lookupLabel(provinces.data, legalAddress?.provinceId ?? '') },
        { label: 'Postal Code', value: orDash(legalAddress?.postalCode) },
      ],
    },
    {
      title: 'Donation Information',
      step: 4,
      rows: [
        { label: 'Collection Address', value: orDash(donations?.collectionAddress) },
        { label: 'Operational Regions', value: lookupLabels(operationalRegions.data, donations?.regionIds ?? []) },
        { label: 'Donation Types', value: lookupLabels(donationTypes.data, donations?.typeIds ?? []) },
        { label: 'Frequency', value: lookupLabel(donationFrequencies.data, donations?.frequencyId ?? '') },
      ],
    },
    {
      title: 'Compliance',
      step: 5,
      rows: [
        { label: 'BBBEE Status', value: lookupLabel(bbbeeStatuses.data, values.bbbeeStatusId ?? '') },
        { label: 'Additional Information', value: orDash(values.additionalInformation) },
      ],
    },
    {
      title: 'Signature',
      step: 6,
      rows: [{ label: 'Signed', value: hasSignature ? 'Yes — signature captured' : 'Not yet signed' }],
    },
  ];

  return (
    <div className="flex flex-col gap-3.5">
      {groups.map((group) => (
        <div key={group.title} className="rounded-xl border border-[#E4E4DE] bg-white p-4.5">
          <div className="mb-2.5 flex items-center justify-between">
            <span className="text-[13px] font-extrabold text-[#16160F]">{group.title}</span>
            <button
              type="button"
              onClick={() => onEditStep(group.step)}
              className="border-b-[1.5px] border-[#FADF01] pb-px text-xs font-bold text-[#16160F]"
            >
              Edit
            </button>
          </div>
          <div className="grid grid-cols-[minmax(0,160px)_1fr] gap-x-4 gap-y-1.5 sm:grid-cols-[200px_1fr]">
            {group.rows.map((row) => (
              <div key={row.label} className="contents">
                <span className="text-[12.5px] font-medium text-[#82827A]">{row.label}</span>
                <span className="text-[12.5px] font-semibold text-[#16160F]">{row.value}</span>
              </div>
            ))}
          </div>
        </div>
      ))}

      <label className="mt-2 flex cursor-pointer items-start gap-2.5">
        <input
          type="checkbox"
          checked={confirmChecked}
          onChange={(e) => onConfirmChange(e.target.checked)}
          className="mt-0.5 h-[18px] w-[18px] cursor-pointer accent-[#16160F]"
        />
        <span className="text-[13px] font-semibold text-[#16160F]">
          I confirm this information is accurate <span className="text-[#D4373A]">*</span>
        </span>
      </label>
      {confirmError && <span className="ml-7 text-[11.5px] font-semibold text-[#D4373A]">{confirmError}</span>}
    </div>
  );
}
