import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm, useWatch, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { paths } from '@/routes/paths';
import {
  useCompanyTypes,
  useEntityTypes,
  useOperationalRegions,
  useDonationTypes,
  useDonationFrequencies,
  useProvinces,
  useBbbeeStatuses,
} from '@/features/lookups';
import { useDonor } from '../hooks/useDonor';
import { useCreateDonor } from '../hooks/useCreateDonor';
import { useUpdateDonor } from '../hooks/useUpdateDonor';
import { createDonorFormSchema, emptyDonorFormValues, type DonorFormValues } from '../schemas/donorFormSchema';
import { donorToFormValues, formValuesToCreateRequest, formValuesToUpdateRequest } from '../schemas/donorFormMapping';
import { applyServerErrors } from '../lib/applyServerErrors';
import { FormSection, FormField, FormSelect, FormTextArea, FormCheckbox, FormCheckboxGroup } from './form';
import type { ApiError } from '../types';

interface DonorFormProps {
  mode: 'create' | 'edit';
  /** Required when mode === 'edit'. */
  donorId?: string;
}

export function DonorForm({ mode, donorId }: DonorFormProps) {
  const navigate = useNavigate();
  const [generalError, setGeneralError] = useState<string | null>(null);

  const donorQuery = useDonor(mode === 'edit' ? donorId : undefined);
  const createDonor = useCreateDonor();
  const updateDonor = useUpdateDonor(donorId ?? '');

  const companyTypes = useCompanyTypes();
  const entityTypes = useEntityTypes();
  const provinces = useProvinces();
  const donationFrequencies = useDonationFrequencies();
  const donationTypes = useDonationTypes();
  const operationalRegions = useOperationalRegions();
  const bbbeeStatuses = useBbbeeStatuses();

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<DonorFormValues>({
    resolver: zodResolver(createDonorFormSchema(mode)),
    defaultValues: emptyDonorFormValues,
  });

  // Pre-populate the form once the existing donor loads (edit mode only).
  useEffect(() => {
    if (mode === 'edit' && donorQuery.data) {
      reset(donorToFormValues(donorQuery.data));
    }
  }, [mode, donorQuery.data, reset]);

  // useWatch (not useForm's `watch`) so this stays compatible with the React
  // Compiler — `watch()` returns a function that can't be memoized safely.
  const hasMarketingContact = useWatch({ control, name: 'hasMarketingContact' });
  const hasAccountsContact = useWatch({ control, name: 'hasAccountsContact' });
  const marketingConsent = useWatch({ control, name: 'marketingConsent' });

  const onSubmit = async (values: DonorFormValues) => {
    setGeneralError(null);
    try {
      if (mode === 'create') {
        const created = await createDonor.mutateAsync(formValuesToCreateRequest(values));
        navigate(paths.donorDetail(created.id));
      } else if (donorId) {
        const updated = await updateDonor.mutateAsync(formValuesToUpdateRequest(values));
        navigate(paths.donorDetail(updated.id));
      }
    } catch (err) {
      const apiError = err as ApiError;
      const mapped = applyServerErrors(apiError, setError);
      setGeneralError(
        mapped
          ? 'Please fix the highlighted fields and try again.'
          : (apiError.response?.data?.message ?? 'Something went wrong. Please try again.')
      );
    }
  };

  if (mode === 'edit' && donorQuery.isPending) {
    return (
      <div className="flex flex-col gap-6">
        <div className="h-96 animate-pulse rounded-2xl bg-[#26261D]" aria-hidden />
        <div className="h-48 animate-pulse rounded-2xl bg-[#26261D]" aria-hidden />
      </div>
    );
  }

  if (mode === 'edit' && donorQuery.isError) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 p-16 text-center">
        <p className="text-sm font-medium text-[#F4F4EE]">Couldn't load this donor</p>
        <p className="text-xs text-[#6B6B60]">
          {donorQuery.error?.response?.data?.message ?? 'Something went wrong.'}
        </p>
        <button
          type="button"
          onClick={() => donorQuery.refetch()}
          className="mt-1 rounded-full border border-[#2B2B23] px-3 py-1 text-xs font-medium text-[#F4F4EE] transition-colors hover:border-[#F4F4EE]"
        >
          Retry
        </button>
      </div>
    );
  }

  const isSaving = isSubmitting || createDonor.isPending || updateDonor.isPending;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
      {generalError && (
        <div className="rounded-lg border border-rose-500/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-300">
          {generalError}
        </div>
      )}

      <FormSection title="Company">
        <FormField
          label="Company name"
          registration={register('company.companyName')}
          error={errors.company?.companyName?.message}
        />
        <FormSelect
          label="Company type"
          placeholder="Select company type"
          registration={register('company.companyTypeId')}
          options={(companyTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
          isLoading={companyTypes.isPending}
          error={errors.company?.companyTypeId?.message}
        />
        <FormField label="Website" registration={register('company.website')} error={errors.company?.website?.message} />
        <FormField
          label="Registered company name"
          registration={register('company.registeredCompanyName')}
          error={errors.company?.registeredCompanyName?.message}
        />
        <FormField
          label="Trading name"
          registration={register('company.tradingName')}
          error={errors.company?.tradingName?.message}
        />
        <FormSelect
          label="Entity type"
          placeholder="Select entity type"
          registration={register('company.entityTypeId')}
          options={(entityTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
          isLoading={entityTypes.isPending}
          error={errors.company?.entityTypeId?.message}
        />
        <FormField
          label="Company registration number"
          registration={register('company.companyRegistrationNumber')}
          error={errors.company?.companyRegistrationNumber?.message}
        />
        <FormField
          label="Income tax number"
          registration={register('company.incomeTaxNumber')}
          error={errors.company?.incomeTaxNumber?.message}
        />
      </FormSection>

      <FormSection title="Primary contact">
        <FormField label="Name" registration={register('primaryContact.name')} error={errors.primaryContact?.name?.message} />
        <FormField
          label="Job title"
          registration={register('primaryContact.jobTitle')}
          error={errors.primaryContact?.jobTitle?.message}
        />
        <FormField label="Phone" registration={register('primaryContact.phone')} error={errors.primaryContact?.phone?.message} />
        <FormField
          label="Email"
          type="email"
          registration={register('primaryContact.email')}
          error={errors.primaryContact?.email?.message}
        />
      </FormSection>

      <FormSection title="Marketing contact" description="Optional — toggle on if this donor has a separate marketing contact.">
        <FormCheckbox
          label="This donor has a marketing contact"
          checked={hasMarketingContact}
          onChange={(checked) => setValue('hasMarketingContact', checked)}
        />
        {hasMarketingContact && (
          <>
            <FormField label="Name" registration={register('marketingContact.name')} error={errors.marketingContact?.name?.message} />
            <FormField
              label="Job title"
              registration={register('marketingContact.jobTitle')}
              error={errors.marketingContact?.jobTitle?.message}
            />
            <FormField
              label="Phone"
              registration={register('marketingContact.phone')}
              error={errors.marketingContact?.phone?.message}
            />
            <FormField
              label="Email"
              type="email"
              registration={register('marketingContact.email')}
              error={errors.marketingContact?.email?.message}
            />
          </>
        )}
      </FormSection>

      <FormSection title="Accounts contact" description="Optional — toggle on if this donor has a separate accounts/billing contact.">
        <FormCheckbox
          label="This donor has an accounts contact"
          checked={hasAccountsContact}
          onChange={(checked) => setValue('hasAccountsContact', checked)}
        />
        {hasAccountsContact && (
          <>
            <FormField label="Name" registration={register('accountsContact.name')} error={errors.accountsContact?.name?.message} />
            <FormField
              label="Job title"
              registration={register('accountsContact.jobTitle')}
              error={errors.accountsContact?.jobTitle?.message}
            />
            <FormField
              label="Phone"
              registration={register('accountsContact.phone')}
              error={errors.accountsContact?.phone?.message}
            />
            <FormField
              label="Email"
              type="email"
              registration={register('accountsContact.email')}
              error={errors.accountsContact?.email?.message}
            />
          </>
        )}
      </FormSection>

      <FormSection title="Legal address">
        <FormField
          label="Street address"
          fullWidth
          registration={register('legalAddress.streetAddress')}
          error={errors.legalAddress?.streetAddress?.message}
        />
        <FormField label="Suburb" registration={register('legalAddress.suburb')} error={errors.legalAddress?.suburb?.message} />
        <FormField label="City" registration={register('legalAddress.city')} error={errors.legalAddress?.city?.message} />
        <FormSelect
          label="Province"
          placeholder="Select province"
          registration={register('legalAddress.provinceId')}
          options={(provinces.data ?? []).map((p) => ({ value: String(p.id), label: p.name }))}
          isLoading={provinces.isPending}
          error={errors.legalAddress?.provinceId?.message}
        />
        <FormField
          label="Postal code"
          registration={register('legalAddress.postalCode')}
          error={errors.legalAddress?.postalCode?.message}
        />
      </FormSection>

      <FormSection title="Donations">
        <FormSelect
          label="Frequency"
          placeholder="Select donation frequency"
          registration={register('donations.frequencyId')}
          options={(donationFrequencies.data ?? []).map((f) => ({ value: String(f.id), label: f.name }))}
          isLoading={donationFrequencies.isPending}
          error={errors.donations?.frequencyId?.message}
        />
        <FormField
          label="Collection address"
          registration={register('donations.collectionAddress')}
          error={errors.donations?.collectionAddress?.message}
        />
        <Controller
          control={control}
          name="donations.typeIds"
          render={({ field }) => (
            <FormCheckboxGroup
              label="Donation types"
              options={(donationTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
              value={field.value}
              onChange={field.onChange}
              isLoading={donationTypes.isPending}
              error={errors.donations?.typeIds?.message}
            />
          )}
        />
        <Controller
          control={control}
          name="donations.regionIds"
          render={({ field }) => (
            <FormCheckboxGroup
              label="Operational regions"
              options={(operationalRegions.data ?? []).map((r) => ({ value: String(r.id), label: r.name }))}
              value={field.value}
              onChange={field.onChange}
              isLoading={operationalRegions.isPending}
              error={errors.donations?.regionIds?.message}
            />
          )}
        />
        <FormTextArea
          label="Operations / logistics details"
          registration={register('donations.operationsLogisticsDetails')}
          error={errors.donations?.operationsLogisticsDetails?.message}
        />
      </FormSection>

      <FormSection title="Compliance">
        <FormSelect
          label="B-BBEE status"
          placeholder="Not verified"
          registration={register('bbbeeStatusId')}
          options={(bbbeeStatuses.data ?? []).map((s) => ({ value: String(s.id), label: s.name }))}
          isLoading={bbbeeStatuses.isPending}
          error={errors.bbbeeStatusId?.message}
        />
      </FormSection>

      <FormSection title="CRM">
        <FormSelect
          label="Relationship manager"
          placeholder="Not yet available"
          registration={register('relationshipManagerId')}
          options={[]}
          disabled
        />
        <FormCheckbox
          label="Marketing consent given"
          checked={marketingConsent}
          onChange={(checked) => setValue('marketingConsent', checked)}
        />
        <FormTextArea
          label="Impact reporting preferences"
          registration={register('impactReportingPreferences')}
          error={errors.impactReportingPreferences?.message}
        />
        <FormTextArea
          label="Additional information"
          registration={register('additionalInformation')}
          error={errors.additionalInformation?.message}
        />
      </FormSection>

      <div className="flex justify-end gap-3">
        <button
          type="button"
          onClick={() => navigate(mode === 'edit' && donorId ? paths.donorDetail(donorId) : paths.donors)}
          className="rounded-full border border-[#2B2B23] px-4 py-2 text-sm font-medium text-[#F4F4EE] transition-colors hover:border-[#F4F4EE]"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isSaving}
          className="rounded-full bg-brand px-5 py-2 text-sm font-bold text-[#16160F] transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSaving ? 'Saving…' : mode === 'create' ? 'Create donor' : 'Save changes'}
        </button>
      </div>
    </form>
  );
}
