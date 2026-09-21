import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import { usePublicCompanyTypes, usePublicEntityTypes } from '../../hooks/usePublicLookups';
import { TextField } from '../fields/TextField';
import { SelectField } from '../fields/SelectField';

interface CompanyStepProps {
  register: UseFormRegister<DonorFormValues>;
  errors: FieldErrors<DonorFormValues>;
}

export function CompanyStep({ register, errors }: CompanyStepProps) {
  const companyTypes = usePublicCompanyTypes();
  const entityTypes = usePublicEntityTypes();

  return (
    <div className="grid grid-cols-1 gap-4.5 sm:grid-cols-2">
      <TextField
        label="Company Name"
        required
        placeholder="e.g. Fresh Fields Wholesale"
        registration={register('company.companyName')}
        error={errors.company?.companyName?.message}
      />
      <SelectField
        label="Company Type"
        required
        placeholder="Select a company type"
        registration={register('company.companyTypeId')}
        options={(companyTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
        isLoading={companyTypes.isPending}
        error={errors.company?.companyTypeId?.message}
      />
      <TextField
        label="Website"
        required
        placeholder="e.g. freshfields.co.za"
        registration={register('company.website')}
        error={errors.company?.website?.message}
      />
      <TextField
        label="Registered Company Name"
        required
        placeholder="Full registered legal name"
        registration={register('company.registeredCompanyName')}
        error={errors.company?.registeredCompanyName?.message}
      />
      <TextField
        label="Trading Name"
        required
        placeholder="If different from registered name"
        registration={register('company.tradingName')}
        error={errors.company?.tradingName?.message}
      />
      <SelectField
        label="Legal Entity Type"
        required
        placeholder="Select an entity type"
        registration={register('company.entityTypeId')}
        options={(entityTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
        isLoading={entityTypes.isPending}
        error={errors.company?.entityTypeId?.message}
      />
      <TextField
        label="Company Registration Number"
        required
        registration={register('company.companyRegistrationNumber')}
        error={errors.company?.companyRegistrationNumber?.message}
      />
      <TextField
        label="Income Tax Number"
        required
        hint="Must not start with 4"
        registration={register('company.incomeTaxNumber')}
        error={errors.company?.incomeTaxNumber?.message}
      />
    </div>
  );
}
