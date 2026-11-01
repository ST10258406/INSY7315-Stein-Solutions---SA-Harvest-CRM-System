import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Controller, useForm, useWatch, type FieldErrors } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  AlertCircle,
  Lock,
} from 'lucide-react';
import { DatePicker } from '@/components/common/DatePicker';
import { Button } from '@/components/ui/button';
import { FormCheckboxGroup, FormField, FormSelect, FormTextArea } from '@/features/donors/components/form';
import { useDonor } from '@/features/donors/hooks/useDonor';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import type { ApiError } from '@/features/donors/types';
import { DonorSelectField } from '@/features/tasks/components/DonorSelectField';
import { taskMinDueDate as todayYmd } from '@/features/tasks/schemas/taskForm.schema';
import { paths } from '@/routes/paths';
import { useCreateVetting, useOfficers, useUpdateVetting, useVetting } from '../hooks';
import * as OPTIONS from '../lib/vettingOptions';
import {
  emptyVettingFormValues,
  formValuesToCreateRequest,
  formValuesToUpdateRequest,
  submissionToFormValues,
} from '../schemas/vettingFormMapping';
import { vettingFormSchema, type VettingDetailsKey, type VettingFormValues } from '../schemas/vettingFormSchema';
import { FIELD_LABELS, FIELD_ORDER, FIELD_SECTION, SECTION_IDS, VETTING_SECTIONS, type SectionMeta } from '../lib/vettingSections';
import { AssessmentFileSlot } from './AssessmentFileSlot';
import { EvidencePhotos, PhotosCapturedBadge } from './EvidencePhotos';
import { AccordionFormLayout, type AccordionFormLayoutHandle } from './form/AccordionFormLayout';
import { FormAccordionSection } from './form/FormAccordionSection';
import { VettingStatusBadge } from './VettingStatusBadge';

const YES_NO = [
  { value: 'true', label: 'Yes' },
  { value: 'false', label: 'No' },
];
const asOptions = (values: readonly string[]) => values.map((v) => ({ value: v, label: v }));

interface VettingFormProps {
  mode: 'create' | 'edit';
  /** Required when mode === 'edit'. */
  submissionId?: string;
  /** Create mode: ?donorId= / ?officerId= / ?siteVisitId= from the query string. */
  prefill?: { donorId?: string; officerId?: string; siteVisitId?: string };
}

export function VettingForm({ mode, submissionId, prefill = {} }: VettingFormProps) {
  const navigate = useNavigate();
  const layoutRef = useRef<AccordionFormLayoutHandle>(null);
  const [generalError, setGeneralError] = useState<string | null>(null);

  const submissionQuery = useVetting(mode === 'edit' ? submissionId : undefined);
  const { data: officers = [], isPending: officersLoading } = useOfficers();
  const createVetting = useCreateVetting();
  const updateVetting = useUpdateVetting(submissionId ?? '');

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    setError,
    setFocus,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<VettingFormValues>({
    resolver: zodResolver(vettingFormSchema),
    defaultValues: emptyVettingFormValues(prefill),
    // Fields in closed sections can't take focus until their section opens; openErrorSections does it.
    shouldFocusError: false,
  });

  useEffect(() => {
    if (mode === 'edit' && submissionQuery.data) reset(submissionToFormValues(submissionQuery.data));
  }, [mode, submissionQuery.data, reset]);

  // A native <select> can't hold a value before its option exists, so re-apply the officer
  // (from ?officerId= or the loaded submission) once the officer list arrives.
  useEffect(() => {
    if (officers.length > 0) setValue('officerId', getValues('officerId'));
  }, [officers, submissionQuery.data, setValue, getValues]);

  // Create mode: picking a donor pre-fills the legal name and province (both stay editable).
  const donorId = useWatch({ control, name: 'donorId' });
  const isRegisteredNpo = useWatch({ control, name: 'details.isRegisteredNpo' });
  const donorQuery = useDonor(mode === 'create' && donorId ? donorId : undefined);
  const prefilledFor = useRef<string | null>(null);
  useEffect(() => {
    const donor = donorQuery.data;
    if (!donor || donor.id !== donorId || prefilledFor.current === donor.id) return;
    prefilledFor.current = donor.id;
    setValue('details.legalName', donor.company.registeredCompanyName || donor.company.companyName, { shouldValidate: true });
    const province = donor.legalAddress?.province?.name;
    if (province && (OPTIONS.PROVINCES as readonly string[]).includes(province))
      setValue('details.province', province, { shouldValidate: true });
  }, [donorQuery.data, donorId, setValue]);

  /** Opens every section with an error, scrolls to the first and focuses its first invalid field. */
  const openErrorSections = (fields: VettingDetailsKey[]) => {
    const ordered = FIELD_ORDER.filter((f) => fields.includes(f));
    if (ordered.length === 0) return;
    layoutRef.current?.openSections([...new Set(ordered.map((f) => FIELD_SECTION[f]))]);
    // After the sections have rendered open.
    requestAnimationFrame(() => {
      document.getElementById(FIELD_SECTION[ordered[0]])?.scrollIntoView?.({ behavior: 'smooth', block: 'start' });
      try {
        setFocus(`details.${ordered[0]}`);
      } catch {
        // Checkbox groups have no focusable ref; the scroll still lands on the section.
      }
    });
  };

  const onInvalid = (formErrors: FieldErrors<VettingFormValues>) =>
    openErrorSections(Object.keys(formErrors.details ?? {}) as VettingDetailsKey[]);

  const onValid = async (values: VettingFormValues) => {
    setGeneralError(null);
    try {
      if (mode === 'create') {
        const created = await createVetting.mutateAsync(formValuesToCreateRequest(values));
        navigate(paths.vettingDetail(created.id));
      } else if (submissionId) {
        await updateVetting.mutateAsync(formValuesToUpdateRequest(values));
        navigate(paths.vettingDetail(submissionId));
      }
    } catch (err) {
      const apiError = err as ApiError;
      const mapped = applyServerErrors(apiError, setError);
      setGeneralError(
        mapped
          ? 'Please fix the highlighted fields and try again.'
          : (apiError.response?.data?.message ?? 'Something went wrong. Please try again.'),
      );
      // "Details.ContactEmail" → "contactEmail", the same path applyServerErrors just set.
      openErrorSections(
        (apiError.response?.data?.errors ?? [])
          .map(({ field }) => field.match(/^Details\.(\w)(\w*)/i))
          .filter((m): m is RegExpMatchArray => m !== null)
          .map((m) => (m[1].toLowerCase() + m[2]) as VettingDetailsKey),
      );
    }
  };

  if (mode === 'edit' && submissionQuery.isPending) {
    return <div className="h-96 animate-pulse rounded-2xl bg-muted" aria-hidden />;
  }

  if (mode === 'edit' && submissionQuery.isError) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 p-16 text-center">
        <p className="text-sm font-medium text-foreground">Couldn't load this vetting submission</p>
        <p className="text-xs text-muted-foreground">{submissionQuery.error?.response?.data?.message ?? 'Something went wrong.'}</p>
        <Button variant="outline" size="sm" onClick={() => submissionQuery.refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  const submission = submissionQuery.data;
  // The API only accepts edits while the form is with the officer (409 otherwise).
  const readOnly = !!submission && !['New', 'Returned'].includes(submission.approvalState);
  // Uploads need a saved submission; create mode shows "Save first" placeholders instead.
  const uploads = { submissionId: submission?.id, files: submission?.files, editable: !readOnly };
  const isSaving = isSubmitting || createVetting.isPending || updateVetting.isPending;
  const detailErrors = (errors.details ?? {}) as Partial<Record<VettingDetailsKey, unknown>>;
  const errorCount = (section: SectionMeta) => section.fields.filter((f) => detailErrors[f]).length;

  const err = (field: VettingDetailsKey) => (detailErrors[field] as { message?: string } | undefined)?.message;
  const yesNo = (field: VettingDetailsKey, label = FIELD_LABELS[field]) => (
    <FormSelect label={label} required registration={register(`details.${field}`)} options={YES_NO} placeholder="Yes or No" error={err(field)} />
  );
  const count = (field: VettingDetailsKey, label = FIELD_LABELS[field]) => (
    <FormField label={label} required type="number" min={0} step={1} registration={register(`details.${field}`, { valueAsNumber: true })} error={err(field)} />
  );
  const choice = (field: VettingDetailsKey, values: readonly string[], label = FIELD_LABELS[field]) => (
    <FormSelect label={label} required registration={register(`details.${field}`)} options={asOptions(values)} placeholder="Select…" error={err(field)} />
  );
  const chips = (field: VettingDetailsKey, values: readonly string[], required = true, label = FIELD_LABELS[field]) => (
    <Controller
      control={control}
      name={`details.${field}`}
      render={({ field: f }) => (
        <FormCheckboxGroup label={label} required={required} options={asOptions(values)} value={f.value as string[]} onChange={f.onChange} error={err(field)} />
      )}
    />
  );

  return (
    <form onSubmit={(event) => handleSubmit(onValid, onInvalid)(event)} noValidate className="flex flex-col gap-4.5">
      {submission?.approvalState === 'Returned' && submission.decisionReason && (
        <div role="alert" className="flex items-start gap-3 rounded-2xl border border-amber-500/40 bg-amber-500/10 p-3.75 px-4.5">
          <AlertCircle className="mt-0.5 h-4.5 w-4.5 shrink-0 text-amber-700 dark:text-amber-400" />
          <p className="m-0 text-[13px] leading-relaxed font-semibold text-foreground">
            <span className="font-extrabold">Returned for corrections:</span> {submission.decisionReason}
          </p>
        </div>
      )}

      {readOnly && (
        <div className="flex items-start gap-3 rounded-2xl border border-border bg-muted/40 p-3.75 px-4.5">
          <Lock className="mt-0.5 h-4.5 w-4.5 shrink-0 text-muted-foreground" />
          <p className="m-0 text-[13px] font-semibold text-foreground">
            This submission is {submission.approvalState.toLowerCase()} and can no longer be edited.
          </p>
        </div>
      )}

      {generalError && (
        <div className="rounded-2xl border border-destructive/40 bg-destructive/10 px-4.5 py-3.75 text-sm text-destructive">{generalError}</div>
      )}

      {/* Header card — not collapsible */}
      <div className="grid grid-cols-1 gap-x-6 gap-y-4 rounded-2xl border border-border bg-card p-5 shadow-sm sm:grid-cols-2">
        {mode === 'create' && !prefill.donorId ? (
          <DonorSelectField value={donorId} onChange={(id) => setValue('donorId', id, { shouldValidate: true })} error={errors.donorId?.message} />
        ) : (
          <div className="flex flex-col gap-2">
            <span className="text-xs font-bold text-foreground">Donor</span>
            <div className="flex h-11 items-center rounded-full border border-border bg-field px-3.75 text-[13.5px] font-semibold text-foreground">
              {submission?.donorCompanyName ?? donorQuery.data?.company.companyName ?? 'Loading…'}
            </div>
          </div>
        )}
        <FormSelect
          label="Officer"
          required
          registration={register('officerId')}
          options={officers.map((o) => ({ value: o.id, label: `${o.fullName} (${o.initials})` }))}
          placeholder="Select an officer"
          isLoading={officersLoading}
          error={errors.officerId?.message}
        />
        {submission && (
          <div className="flex items-center gap-3 sm:col-span-2">
            <span className="text-sm font-extrabold tracking-tight text-foreground">{submission.formReference}</span>
            <VettingStatusBadge kind="approval" status={submission.approvalState} />
          </div>
        )}
      </div>

      <AccordionFormLayout ref={layoutRef} sectionIds={SECTION_IDS} defaultOpen={mode === 'create' ? [SECTION_IDS[0]] : []}>
        {VETTING_SECTIONS.map((section, index) => (
          <FormAccordionSection
            key={section.id}
            id={section.id}
            number={index + 1}
            title={section.title}
            icon={section.icon}
            errorCount={errorCount(section)}
            badge={section.id === 'photos' && submission ? <PhotosCapturedBadge photos={submission.photos} /> : undefined}
          >
            {section.id === 'organisation' && (
              <>
                <FormField label={FIELD_LABELS.legalName} required fullWidth registration={register('details.legalName')} error={err('legalName')} />
                <FormField label={FIELD_LABELS.contactName} required registration={register('details.contactName')} error={err('contactName')} />
                <FormField label={FIELD_LABELS.contactEmail} required type="email" registration={register('details.contactEmail')} error={err('contactEmail')} />
                <FormField label={FIELD_LABELS.contactPhone} required type="tel" registration={register('details.contactPhone')} error={err('contactPhone')} />
                <FormField label={FIELD_LABELS.website} type="url" placeholder="https://" registration={register('details.website')} error={err('website')} />
              </>
            )}
            {section.id === 'location' && (
              <>
                <FormTextArea label={FIELD_LABELS.addressPhysical} registration={register('details.addressPhysical')} error={err('addressPhysical')} />
                <FormTextArea label={FIELD_LABELS.addressDetail} hint="As much detail as possible, especially if rural." registration={register('details.addressDetail')} error={err('addressDetail')} />
                {choice('province', OPTIONS.PROVINCES)}
                <FormField
                  label={FIELD_LABELS.what3Words}
                  required
                  placeholder="///apple.orange.banana"
                  hint="Use the What3Words address in rural areas or informal settlements."
                  registration={register('details.what3Words')}
                  error={err('what3Words')}
                />
              </>
            )}
            {section.id === 'programmes' && (
              <>
                {chips('coreFocusAreas', OPTIONS.CORE_FOCUS_AREAS)}
                {chips('targetPopulation', OPTIONS.TARGET_POPULATIONS)}
                <FormTextArea label={FIELD_LABELS.programmesSummary} required registration={register('details.programmesSummary')} error={err('programmesSummary')} />
                {choice('distributionChannel', OPTIONS.DISTRIBUTION_CHANNELS)}
              </>
            )}
            {section.id === 'staffing' && (
              <>
                {count('fullTimeFemales')}
                {count('fullTimeMales')}
                {count('volunteersCount')}
              </>
            )}
            {section.id === 'registration' && (
              <>
                {yesNo('isRegisteredNpo')}
                {yesNo('isRegisteredDsd')}
                {/* Matches the auto flag raised on submit (#157). */}
                <AssessmentFileSlot {...uploads} slot="npo-certificate" hint={isRegisteredNpo === 'true' ? 'Required if registered NPO' : undefined} />
                <AssessmentFileSlot {...uploads} slot="pbo-certificate" />
              </>
            )}
            {section.id === 'demographics' && (
              <>
                {chips('beneficiaryRaces', OPTIONS.RACES)}
                {chips('beneficiaryGenders', OPTIONS.GENDERS)}
                {chips('beneficiaryAgeGroups', OPTIONS.AGE_GROUPS)}
              </>
            )}
            {section.id === 'feeding' && (
              <>
                {choice('feedingFrequency', OPTIONS.FEEDING_FREQUENCIES)}
                {choice('saHarvestReliance', OPTIONS.RELIANCE_LEVELS)}
                {choice('transportCapacity', OPTIONS.TRANSPORT_CAPACITIES)}
                {count('totalPeopleServed')}
                {count('femalesServed')}
                {count('malesServed')}
                {count('africanServed')}
                {count('colouredServed')}
                {count('indianAsianServed')}
                {count('whiteServed')}
                <SubtotalHint control={control} />
                {chips('mealsProvided', OPTIONS.MEALS)}
                {chips('feedingDays', OPTIONS.DAYS)}
                <div className="flex flex-col gap-2">
                  <label htmlFor="details.lastFedDate" className="text-xs font-bold text-foreground">
                    {FIELD_LABELS.lastFedDate}
                  </label>
                  <Controller
                    control={control}
                    name="details.lastFedDate"
                    render={({ field }) => (
                      <DatePicker id="details.lastFedDate" variant="field" value={field.value} onChange={field.onChange} max={todayYmd()} clearable invalid={!!err('lastFedDate')} />
                    )}
                  />
                  {err('lastFedDate') && <span className="pl-3.75 text-xs text-destructive">{err('lastFedDate')}</span>}
                </div>
              </>
            )}
            {section.id === 'facilities' && (
              <>
                {chips('foodStorageTypes', OPTIONS.STORAGE_TYPES)}
                {yesNo('kitchenClean')}
                {yesNo('waterAccess')}
                {yesNo('toiletFacilities')}
                {yesNo('pestFree')}
                {chips('infrastructureChecks', OPTIONS.INFRASTRUCTURE_CHECKS, false)}
                <AssessmentFileSlot {...uploads} slot="kitchen-storage-photos" />
              </>
            )}
            {section.id === 'access' && (
              <>
                {yesNo('deliveryAccessEase')}
                {yesNo('parkingAndSecurity')}
                <FormField label={FIELD_LABELS.policeStationProximity} registration={register('details.policeStationProximity')} error={err('policeStationProximity')} />
              </>
            )}
            {section.id === 'capacity' && (
              <>
                <FormTextArea label={FIELD_LABELS.officerComments} registration={register('details.officerComments')} error={err('officerComments')} />
                <FormTextArea label={FIELD_LABELS.proposalCapabilities} registration={register('details.proposalCapabilities')} error={err('proposalCapabilities')} />
                <FormTextArea label={FIELD_LABELS.digitalCapabilities} registration={register('details.digitalCapabilities')} error={err('digitalCapabilities')} />
                <AssessmentFileSlot {...uploads} slot="facility-photos" />
              </>
            )}
            {section.id === 'documents' && (
              <>
                {yesNo('slaSigned')}
                {yesNo('consentFormSigned')}
                {yesNo('policyDocumentPresent')}
                <AssessmentFileSlot {...uploads} slot="certificates" />
              </>
            )}
            {section.id === 'photos' && (
              <>
                <p className="m-0 text-[13px] font-medium text-muted-foreground sm:col-span-2">
                  All four photos are needed before the form can be submitted for review.
                </p>
                <EvidencePhotos submissionId={uploads.submissionId} photos={submission?.photos ?? []} editable={uploads.editable} />
              </>
            )}
          </FormAccordionSection>
        ))}
      </AccordionFormLayout>

      <div className="flex flex-wrap items-center gap-3 px-1 py-1">
        <p className="m-0 text-[12.5px] font-medium text-muted-foreground">
          Fields marked <span className="font-bold text-destructive">*</span> are required. Saving keeps the form as New until it is submitted.
        </p>
        <div className="ml-auto flex items-center gap-2.5">
          <Button
            variant="outline"
            type="button"
            onClick={() => navigate(mode === 'edit' && submissionId ? paths.vettingDetail(submissionId) : paths.vetting)}
          >
            Cancel
          </Button>
          {!readOnly && (
            <Button type="submit" disabled={isSaving}>
              {isSaving ? 'Saving…' : mode === 'create' ? 'Save vetting' : 'Save changes'}
            </Button>
          )}
        </div>
      </div>
    </form>
  );
}

/** Live, non-blocking check of the section 7 subtotals. A mismatch becomes an auto flag on submit, not an error. */
function SubtotalHint({ control }: { control: ReturnType<typeof useForm<VettingFormValues>>['control'] }) {
  const d = useWatch({ control, name: 'details' });
  const n = (v: unknown) => (typeof v === 'number' && Number.isFinite(v) ? v : 0);
  const total = n(d.totalPeopleServed);
  const lines = [
    ['Gender', n(d.femalesServed) + n(d.malesServed)],
    ['Race', n(d.africanServed) + n(d.colouredServed) + n(d.indianAsianServed) + n(d.whiteServed)],
  ] as const;

  return (
    <div className="flex flex-col gap-1 sm:col-span-2" aria-live="polite">
      {lines.map(([label, subtotal]) => (
        <span
          key={label}
          data-mismatch={subtotal !== total}
          className={`text-xs font-semibold ${subtotal === total ? 'text-muted-foreground' : 'text-amber-700 dark:text-amber-400'}`}
        >
          {label} subtotal {subtotal} of {total}
        </span>
      ))}
    </div>
  );
}
