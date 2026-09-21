import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import { usePublicProvinces } from '../../hooks/usePublicLookups';
import { TextField } from '../fields/TextField';
import { SelectField } from '../fields/SelectField';

interface AddressStepProps {
  register: UseFormRegister<DonorFormValues>;
  errors: FieldErrors<DonorFormValues>;
}

export function AddressStep({ register, errors }: AddressStepProps) {
  const provinces = usePublicProvinces();

  return (
    <div className="grid grid-cols-1 gap-4.5 sm:grid-cols-2">
      <TextField
        label="Street Name & Number"
        required
        fullWidth
        registration={register('legalAddress.streetAddress')}
        error={errors.legalAddress?.streetAddress?.message}
      />
      <TextField label="Suburb" required registration={register('legalAddress.suburb')} error={errors.legalAddress?.suburb?.message} />
      <TextField label="City" required placeholder="e.g. Cape Town" registration={register('legalAddress.city')} error={errors.legalAddress?.city?.message} />
      <SelectField
        label="Province"
        required
        placeholder="Select a province"
        registration={register('legalAddress.provinceId')}
        options={(provinces.data ?? []).map((p) => ({ value: String(p.id), label: p.name }))}
        isLoading={provinces.isPending}
        error={errors.legalAddress?.provinceId?.message}
      />
      <TextField label="Postal Code" required registration={register('legalAddress.postalCode')} error={errors.legalAddress?.postalCode?.message} />
    </div>
  );
}
