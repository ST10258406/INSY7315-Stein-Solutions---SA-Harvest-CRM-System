import type { Control, FieldErrors, UseFormRegister } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import { usePublicDonationFrequencies, usePublicDonationTypes, usePublicOperationalRegions } from '../../hooks/usePublicLookups';
import { TextAreaField } from '../fields/TextAreaField';
import { SelectField } from '../fields/SelectField';
import { ChipMultiSelect } from '../fields/ChipMultiSelect';

interface DonationsStepProps {
  register: UseFormRegister<DonorFormValues>;
  control: Control<DonorFormValues>;
  errors: FieldErrors<DonorFormValues>;
}

export function DonationsStep({ register, control, errors }: DonationsStepProps) {
  const donationFrequencies = usePublicDonationFrequencies();
  const donationTypes = usePublicDonationTypes();
  const operationalRegions = usePublicOperationalRegions();

  return (
    <div className="flex flex-col gap-5.5">
      <TextAreaField
        label="Collection / Pickup Address"
        required
        hint="Where should we collect donations from? This can differ from your registered address above."
        placeholder="Street address for collections"
        registration={register('donations.collectionAddress')}
        error={errors.donations?.collectionAddress?.message}
      />

      <Controller
        control={control}
        name="donations.regionIds"
        render={({ field }) => (
          <ChipMultiSelect
            label="Operational Regions"
            required
            options={(operationalRegions.data ?? []).map((r) => ({ value: String(r.id), label: r.name }))}
            value={field.value}
            onChange={field.onChange}
            isLoading={operationalRegions.isPending}
            error={errors.donations?.regionIds?.message}
          />
        )}
      />

      <Controller
        control={control}
        name="donations.typeIds"
        render={({ field }) => (
          <ChipMultiSelect
            label="Donation Types"
            required
            options={(donationTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
            value={field.value}
            onChange={field.onChange}
            isLoading={donationTypes.isPending}
            error={errors.donations?.typeIds?.message}
          />
        )}
      />

      <SelectField
        label="Donation Frequency"
        required
        placeholder="Select a frequency"
        registration={register('donations.frequencyId')}
        options={(donationFrequencies.data ?? []).map((f) => ({ value: String(f.id), label: f.name }))}
        isLoading={donationFrequencies.isPending}
        error={errors.donations?.frequencyId?.message}
      />

      <TextAreaField
        label="Operations / Logistics Details"
        optional
        hint="Warehouse contact, preferred collection times, access instructions, etc."
        registration={register('donations.operationsLogisticsDetails')}
        error={errors.donations?.operationsLogisticsDetails?.message}
      />
    </div>
  );
}
