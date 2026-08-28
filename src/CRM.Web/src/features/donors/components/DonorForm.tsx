import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm, useWatch, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Building2,
  Users,
  MapPin,
  Truck,
  ShieldCheck,
  MessageSquare,
  AlertCircle,
  Search,
  type LucideIcon,
} from 'lucide-react';
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

interface SectionMeta {
  id: string;
  num: string;
  label: string;
  icon: LucideIcon;
}

const SECTIONS: SectionMeta[] = [
  { id: 'company', num: '1', label: 'Company', icon: Building2 },
  { id: 'contacts', num: '2', label: 'Contacts', icon: Users },
  { id: 'address', num: '3', label: 'Legal Address', icon: MapPin },
  { id: 'donation', num: '4', label: 'Donation', icon: Truck },
  { id: 'compliance', num: '5', label: 'Compliance', icon: ShieldCheck },
  { id: 'crm', num: '6', label: 'CRM Details', icon: MessageSquare },
];

export function DonorForm({ mode, donorId }: DonorFormProps) {
  const navigate = useNavigate();
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState(SECTIONS[0].id);

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

  // Mirrors requiredOnCreate in donorFormSchema.ts — these fields are only
  // NotEmpty on create; edit lets a PATCH omit/blank them (legacy data).
  const requiredOnCreate = mode === 'create';

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

  const scrollToSection = (id: string) => {
    setActiveSection(id);
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  // Keep the section nav in sync while the user scrolls the form, not just
  // when they click a nav item. The sections render inside AppLayout's
  // <main overflow-y-auto> (see AppLayout.tsx) rather than in a scroll
  // container DonorForm owns itself, so we walk up to find whichever
  // ancestor actually scrolls instead of assuming `window`.
  useEffect(() => {
    if (mode === 'edit' && (donorQuery.isPending || donorQuery.isError)) return;

    const sectionEls = SECTIONS.map((s) => document.getElementById(s.id)).filter(
      (el): el is HTMLElement => el !== null
    );
    if (sectionEls.length === 0) return;

    function findScrollParent(el: HTMLElement): HTMLElement | null {
      let node = el.parentElement;
      while (node) {
        const overflowY = getComputedStyle(node).overflowY;
        if (overflowY === 'auto' || overflowY === 'scroll') return node;
        node = node.parentElement;
      }
      return null;
    }

    const scrollParent = findScrollParent(sectionEls[0]);
    if (!scrollParent) return;

    // A section is "current" once its top has scrolled up to (or past) this
    // many pixels below the scroll container's own top edge.
    const ACTIVE_LINE_OFFSET = 32;
    let ticking = false;

    function updateActiveSection() {
      const containerTop = scrollParent!.getBoundingClientRect().top;
      let current = sectionEls[0].id;
      for (const el of sectionEls) {
        if (el.getBoundingClientRect().top - containerTop <= ACTIVE_LINE_OFFSET) {
          current = el.id;
        }
      }
      setActiveSection(current);
      ticking = false;
    }

    function onScroll() {
      if (ticking) return;
      ticking = true;
      requestAnimationFrame(updateActiveSection);
    }

    updateActiveSection();
    scrollParent.addEventListener('scroll', onScroll, { passive: true });
    return () => scrollParent.removeEventListener('scroll', onScroll);
  }, [mode, donorQuery.isPending, donorQuery.isError]);

  if (mode === 'edit' && donorQuery.isPending) {
    return (
      <div className="flex flex-col gap-6">
        <div className="h-96 animate-pulse rounded-2xl bg-muted" aria-hidden />
        <div className="h-48 animate-pulse rounded-2xl bg-muted" aria-hidden />
      </div>
    );
  }

  if (mode === 'edit' && donorQuery.isError) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 p-16 text-center">
        <p className="text-sm font-medium text-foreground">Couldn't load this donor</p>
        <p className="text-xs text-muted-foreground">
          {donorQuery.error?.response?.data?.message ?? 'Something went wrong.'}
        </p>
        <button
          type="button"
          onClick={() => donorQuery.refetch()}
          className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
        >
          Retry
        </button>
      </div>
    );
  }

  const isSaving = isSubmitting || createDonor.isPending || updateDonor.isPending;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4.5">
      {mode === 'create' && (
        <div className="flex items-start gap-3 rounded-2xl border border-brand/40 bg-brand/10 p-3.75 px-4.5">
          <AlertCircle className="mt-0.5 h-4.5 w-4.5 shrink-0 text-foreground" />
          <p className="m-0 text-[13px] leading-relaxed font-semibold text-foreground">
            This donor will be marked as <span className="font-extrabold">Pending Review</span> until an Admin
            approves it.
          </p>
        </div>
      )}

      {generalError && (
        <div className="rounded-2xl border border-destructive/40 bg-destructive/10 px-4.5 py-3.75 text-sm text-destructive">
          {generalError}
        </div>
      )}

      {/* Section nav + form content */}
      <div className="grid grid-cols-1 items-start gap-6 md:grid-cols-[210px_minmax(0,1fr)]">
        <nav className="sticky top-6 flex flex-col gap-0.5 rounded-2xl border border-border bg-muted/40 p-2.5 md:self-start">
          {SECTIONS.map((s) => {
            const isActive = activeSection === s.id;
            const Icon = s.icon;
            return (
              <button
                key={s.id}
                type="button"
                onClick={() => scrollToSection(s.id)}
                className={`flex cursor-pointer items-center gap-2.25 rounded-xl p-2.25 pl-1 text-[13px] transition-all ${
                  isActive
                    ? 'bg-card font-bold text-foreground shadow-xs'
                    : 'bg-transparent font-semibold text-foreground hover:bg-muted'
                }`}
              >
                <span className={`h-4.5 w-0.75 shrink-0 rounded-full ${isActive ? 'bg-brand' : 'bg-transparent'}`} />
                <span
                  className={`flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10.5px] font-bold ${
                    isActive ? 'bg-foreground text-background' : 'bg-muted text-muted-foreground'
                  }`}
                >
                  {s.num}
                </span>
                <Icon className="hidden h-3.5 w-3.5 shrink-0 sm:block" />
                <span>{s.label}</span>
              </button>
            );
          })}
        </nav>

        <div className="flex min-w-0 flex-col gap-4.5">
          <FormSection id="company" title="Company Information" description="Legal and trading identity of the donor organisation." icon={Building2}>
            <FormField
              label="Company name"
              required
              registration={register('company.companyName')}
              error={errors.company?.companyName?.message}
            />
            <FormSelect
              label="Company type"
              required
              placeholder="Select company type"
              registration={register('company.companyTypeId')}
              options={(companyTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
              isLoading={companyTypes.isPending}
              error={errors.company?.companyTypeId?.message}
            />
            <FormField
              label="Website"
              required={requiredOnCreate}
              registration={register('company.website')}
              error={errors.company?.website?.message}
            />
            <FormField
              label="Registered company name"
              required={requiredOnCreate}
              registration={register('company.registeredCompanyName')}
              error={errors.company?.registeredCompanyName?.message}
            />
            <FormField
              label="Trading name"
              required={requiredOnCreate}
              registration={register('company.tradingName')}
              error={errors.company?.tradingName?.message}
            />
            <FormSelect
              label="Entity type"
              required
              placeholder="Select entity type"
              registration={register('company.entityTypeId')}
              options={(entityTypes.data ?? []).map((t) => ({ value: String(t.id), label: t.name }))}
              isLoading={entityTypes.isPending}
              error={errors.company?.entityTypeId?.message}
            />
            <FormField
              label="Company registration number"
              required={requiredOnCreate}
              registration={register('company.companyRegistrationNumber')}
              error={errors.company?.companyRegistrationNumber?.message}
            />
            <FormField
              label="Income tax number"
              required={requiredOnCreate}
              hint="Must not start with 4 — that's a VAT number."
              registration={register('company.incomeTaxNumber')}
              error={errors.company?.incomeTaxNumber?.message}
            />
          </FormSection>

          {/* Contacts */}
          <section id="contacts" className="rounded-2xl border border-border bg-muted/40 p-5">
            <div className="mb-4 flex items-center gap-2.75">
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground">
                <Users className="h-4 w-4" />
              </div>
              <div>
                <h3 className="m-0 text-base font-bold tracking-tight text-foreground">Contacts</h3>
                <p className="m-0 mt-0.5 text-xs font-medium text-muted-foreground">
                  Who we speak to for donations, marketing, and accounts.
                </p>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-3.5 md:grid-cols-3">
              {/* Primary Contact */}
              <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
                <div className="mb-4 flex items-center gap-1.75">
                  <h4 className="m-0 text-[13.5px] font-extrabold text-foreground">Primary Contact</h4>
                  <span className="rounded-md bg-destructive/10 px-2 py-0.75 text-[10px] font-bold text-destructive">
                    Required
                  </span>
                </div>
                <div className="flex flex-col gap-3.5">
                  <FormField label="Name" required registration={register('primaryContact.name')} error={errors.primaryContact?.name?.message} />
                  <FormField label="Job title" registration={register('primaryContact.jobTitle')} error={errors.primaryContact?.jobTitle?.message} />
                  <FormField label="Phone" required registration={register('primaryContact.phone')} error={errors.primaryContact?.phone?.message} />
                  <FormField label="Email" required type="email" registration={register('primaryContact.email')} error={errors.primaryContact?.email?.message} />
                </div>
              </div>

              {/* Marketing Contact */}
              <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
                <div className="mb-4 flex items-center justify-between gap-2">
                  <div className="flex items-center gap-1.75">
                    <h4 className="m-0 text-[13.5px] font-extrabold text-foreground">Marketing Contact</h4>
                    <span className="rounded-md bg-muted px-2 py-0.75 text-[10px] font-bold text-muted-foreground">
                      Optional
                    </span>
                  </div>
                  <button
                    type="button"
                    role="switch"
                    aria-checked={hasMarketingContact}
                    aria-label="Include a marketing contact"
                    onClick={() => setValue('hasMarketingContact', !hasMarketingContact)}
                    className={`relative h-6 w-11 shrink-0 cursor-pointer rounded-full border transition-colors ${
                      hasMarketingContact ? 'border-brand bg-brand' : 'border-border bg-muted'
                    }`}
                  >
                    <span
                      className={`absolute top-[2px] h-4.5 w-4.5 rounded-full bg-card shadow-sm transition-all ${
                        hasMarketingContact ? 'left-[21px]' : 'left-[2px]'
                      }`}
                    />
                  </button>
                </div>
                {hasMarketingContact ? (
                  <div className="flex flex-col gap-3.5">
                    <FormField label="Name" required registration={register('marketingContact.name')} error={errors.marketingContact?.name?.message} />
                    <FormField label="Job title" registration={register('marketingContact.jobTitle')} error={errors.marketingContact?.jobTitle?.message} />
                    <FormField label="Phone" required registration={register('marketingContact.phone')} error={errors.marketingContact?.phone?.message} />
                    <FormField label="Email" required type="email" registration={register('marketingContact.email')} error={errors.marketingContact?.email?.message} />
                  </div>
                ) : (
                  <p className="m-0 text-xs font-medium text-muted-foreground">
                    Toggle on if this donor has a separate marketing contact.
                  </p>
                )}
              </div>

              {/* Accounts Contact */}
              <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
                <div className="mb-4 flex items-center justify-between gap-2">
                  <div className="flex items-center gap-1.75">
                    <h4 className="m-0 text-[13.5px] font-extrabold text-foreground">Accounts Contact</h4>
                    <span className="rounded-md bg-muted px-2 py-0.75 text-[10px] font-bold text-muted-foreground">
                      Optional
                    </span>
                  </div>
                  <button
                    type="button"
                    role="switch"
                    aria-checked={hasAccountsContact}
                    aria-label="Include an accounts contact"
                    onClick={() => setValue('hasAccountsContact', !hasAccountsContact)}
                    className={`relative h-6 w-11 shrink-0 cursor-pointer rounded-full border transition-colors ${
                      hasAccountsContact ? 'border-brand bg-brand' : 'border-border bg-muted'
                    }`}
                  >
                    <span
                      className={`absolute top-[2px] h-4.5 w-4.5 rounded-full bg-card shadow-sm transition-all ${
                        hasAccountsContact ? 'left-[21px]' : 'left-[2px]'
                      }`}
                    />
                  </button>
                </div>
                {hasAccountsContact ? (
                  <div className="flex flex-col gap-3.5">
                    <FormField label="Name" required registration={register('accountsContact.name')} error={errors.accountsContact?.name?.message} />
                    <FormField label="Job title" registration={register('accountsContact.jobTitle')} error={errors.accountsContact?.jobTitle?.message} />
                    <FormField label="Phone" required registration={register('accountsContact.phone')} error={errors.accountsContact?.phone?.message} />
                    <FormField label="Email" required type="email" registration={register('accountsContact.email')} error={errors.accountsContact?.email?.message} />
                  </div>
                ) : (
                  <p className="m-0 text-xs font-medium text-muted-foreground">
                    Toggle on if this donor has a separate accounts/billing contact.
                  </p>
                )}
              </div>
            </div>
          </section>

          <FormSection id="address" title="Registered / Legal Address" description="All fields required for Section 18A certificates." icon={MapPin}>
            <FormField
              label="Street address"
              fullWidth
              required={requiredOnCreate}
              registration={register('legalAddress.streetAddress')}
              error={errors.legalAddress?.streetAddress?.message}
            />
            <FormField label="Suburb" required={requiredOnCreate} registration={register('legalAddress.suburb')} error={errors.legalAddress?.suburb?.message} />
            <FormField label="City" required={requiredOnCreate} registration={register('legalAddress.city')} error={errors.legalAddress?.city?.message} />
            <FormSelect
              label="Province"
              required
              placeholder="Select province"
              registration={register('legalAddress.provinceId')}
              options={(provinces.data ?? []).map((p) => ({ value: String(p.id), label: p.name }))}
              isLoading={provinces.isPending}
              error={errors.legalAddress?.provinceId?.message}
            />
            <FormField
              label="Postal code"
              required={requiredOnCreate}
              registration={register('legalAddress.postalCode')}
              error={errors.legalAddress?.postalCode?.message}
            />
          </FormSection>

          <FormSection id="donation" title="Donation Information" description="What they donate, where from, and how often." icon={Truck}>
            <FormSelect
              label="Frequency"
              required
              placeholder="Select donation frequency"
              registration={register('donations.frequencyId')}
              options={(donationFrequencies.data ?? []).map((f) => ({ value: String(f.id), label: f.name }))}
              isLoading={donationFrequencies.isPending}
              error={errors.donations?.frequencyId?.message}
            />
            <FormField
              label="Collection address"
              required={requiredOnCreate}
              registration={register('donations.collectionAddress')}
              error={errors.donations?.collectionAddress?.message}
            />
            <Controller
              control={control}
              name="donations.typeIds"
              render={({ field }) => (
                <FormCheckboxGroup
                  label="Donation types"
                  required
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
                  required
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
              hint="Loading bay hours, pallet requirements, cold chain notes."
              registration={register('donations.operationsLogisticsDetails')}
              error={errors.donations?.operationsLogisticsDetails?.message}
            />
          </FormSection>

          <FormSection id="compliance" title="Compliance" description="B-BBEE standing and supporting documentation." icon={ShieldCheck}>
            <FormSelect
              label="B-BBEE status"
              placeholder="Not verified"
              registration={register('bbbeeStatusId')}
              options={(bbbeeStatuses.data ?? []).map((s) => ({ value: String(s.id), label: s.name }))}
              isLoading={bbbeeStatuses.isPending}
              error={errors.bbbeeStatusId?.message}
            />
            <div className="flex flex-col gap-2 sm:col-span-2">
              <span className="text-xs font-bold text-foreground">B-BBEE certificate</span>
              <p className="m-0 rounded-2xl border border-dashed border-border bg-field p-4 text-[11.5px] font-medium text-muted-foreground">
                Certificate upload is available once this donor has been created — from the Compliance tab on the
                donor's profile.
              </p>
            </div>
          </FormSection>

          <FormSection id="crm" title="CRM & Internal Details" description="Ownership, consent, and follow-up tracking." icon={MessageSquare}>
            <label className="flex flex-col gap-2 sm:col-span-2">
              <span className="flex items-center gap-1 text-xs font-bold text-foreground">
                <span>Relationship manager</span>
              </span>
              <div className="flex h-11 items-center gap-2.5 rounded-full border border-border bg-field px-3.75 opacity-50">
                <Search className="h-3.75 w-3.75 shrink-0 text-muted-foreground" />
                <select
                  {...register('relationshipManagerId')}
                  disabled
                  className="w-full cursor-not-allowed border-none bg-transparent text-[13.5px] font-medium text-foreground outline-none"
                >
                  <option value="">Not yet available</option>
                </select>
              </div>
            </label>

            <FormCheckbox
              label="Marketing consent given"
              description="Required for POPIA compliance."
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

          {/* Footer actions */}
          <div className="flex flex-wrap items-center gap-3 px-1 py-1">
            <p className="m-0 text-[12.5px] font-medium text-muted-foreground">
              Fields marked <span className="font-bold text-destructive">*</span> are required before this donor can
              be submitted for approval.
            </p>
            <div className="ml-auto flex items-center gap-2.5">
              <button
                type="button"
                onClick={() => navigate(mode === 'edit' && donorId ? paths.donorDetail(donorId) : paths.donors)}
                className="rounded-full border border-border px-4 py-2 text-sm font-medium text-foreground transition-colors hover:border-foreground/60"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSaving}
                className="h-11 rounded-full bg-brand px-6.5 text-sm font-bold text-primary-foreground transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSaving ? 'Saving…' : mode === 'create' ? 'Create donor' : 'Save changes'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </form>
  );
}
