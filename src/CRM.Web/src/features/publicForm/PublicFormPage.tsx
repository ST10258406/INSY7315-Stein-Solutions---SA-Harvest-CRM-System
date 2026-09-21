import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import type { Path } from 'react-hook-form';
import { createDonorFormSchema, emptyDonorFormValues, type DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import type { ApiError } from '@/features/donors/types';
import { PublicFormLayout } from './components/PublicFormLayout';
import { PublicPageShell } from './components/PublicPageShell';
import { PublicCard } from './components/PublicCard';
import { DocumentUploadStep } from './components/DocumentUploadStep';
import { ConfirmationScreen } from './components/ConfirmationScreen';
import { usePublicFormSteps } from './hooks/usePublicFormSteps';
import { useSubmitPublicDonor, type SubmitPublicDonorResponse } from './hooks/useSubmitPublicDonor';
import { formValuesToPublicSubmitRequest } from './lib/publicDonorMapping';
import { CompanyStep } from './components/steps/CompanyStep';
import { ContactsStep } from './components/steps/ContactsStep';
import { AddressStep } from './components/steps/AddressStep';
import { DonationsStep } from './components/steps/DonationsStep';
import { ComplianceStep } from './components/steps/ComplianceStep';
import { SignatureStep } from './components/steps/SignatureStep';
import { ReviewStep } from './components/steps/ReviewStep';

// Step grouping/order matches the approved design (SA Harvest Donor
// Onboarding mockup).
const STEP_LABELS = ['Company Info', 'Contacts', 'Address', 'Donations', 'Compliance', 'Signature', 'Review'];
const STEP_TITLES = ['Company Information', 'Contacts', 'Registered Address', 'Donation Information', 'Compliance', 'Signature', 'Review'];
const STEP_DESCRIPTIONS = [
  'Tell us about your company. Fields marked * are required.',
  'Who should we speak to at your company?',
  "Your company's registered address.",
  'Help us plan collections and match your donations to need.',
  'All fields on this step are optional.',
  'Please sign to confirm the information provided is accurate.',
  'Please check your details before submitting.',
];

// Field paths validated before letting the user advance off each step — see
// createDonorFormSchema in features/donors/schemas/donorFormSchema.ts for
// the rules themselves (reused as-is; this form doesn't duplicate them).
// Step 5 (Compliance) is entirely optional, so there's nothing to trigger.
// Step 6 (Signature) isn't an RHF field at all — see handleNext's dedicated
// branch for it below.
const STEP_FIELD_PATHS: Record<number, Path<DonorFormValues>[]> = {
  1: [
    'company.companyName',
    'company.companyTypeId',
    'company.website',
    'company.registeredCompanyName',
    'company.tradingName',
    'company.entityTypeId',
    'company.companyRegistrationNumber',
    'company.incomeTaxNumber',
  ],
  2: [
    'primaryContact.name',
    'primaryContact.phone',
    'primaryContact.email',
    'marketingContact.name',
    'marketingContact.phone',
    'marketingContact.email',
    'accountsContact.name',
    'accountsContact.phone',
    'accountsContact.email',
  ],
  3: ['legalAddress.streetAddress', 'legalAddress.suburb', 'legalAddress.city', 'legalAddress.provinceId', 'legalAddress.postalCode'],
  4: ['donations.frequencyId', 'donations.collectionAddress', 'donations.typeIds', 'donations.regionIds'],
  5: [],
};

const SIGNATURE_STEP = 6;

type Phase = 'form' | 'uploading-document' | 'complete';

export default function PublicFormPage() {
  const { currentStep, isFirst, isLast, goNext, goBack, goToStep } = usePublicFormSteps(STEP_LABELS.length);
  const [confirmChecked, setConfirmChecked] = useState(false);
  const [confirmError, setConfirmError] = useState<string | undefined>();
  const [signatureDataUrl, setSignatureDataUrl] = useState<string | null>(null);
  const [signatureError, setSignatureError] = useState<string | undefined>();
  const [phase, setPhase] = useState<Phase>('form');
  const [submissionResult, setSubmissionResult] = useState<SubmitPublicDonorResponse | null>(null);

  const submitDonor = useSubmitPublicDonor();

  const {
    register,
    control,
    trigger,
    setValue,
    getValues,
    formState: { errors },
  } = useForm<DonorFormValues>({
    resolver: zodResolver(createDonorFormSchema('create')),
    defaultValues: emptyDonorFormValues,
    mode: 'onBlur',
  });

  async function handleNext() {
    setConfirmError(undefined);

    if (currentStep === SIGNATURE_STEP) {
      if (!signatureDataUrl) {
        setSignatureError('A signature is required before you can continue.');
        return;
      }
      goNext();
      return;
    }

    if (currentStep === STEP_LABELS.length) {
      if (!confirmChecked) {
        setConfirmError('Please confirm before submitting.');
        return;
      }
      const valid = await trigger();
      if (!valid) {
        toast.error('Please fix the highlighted fields before submitting.');
        return;
      }
      // Guarded already by the signature-step gate above — this is a
      // belt-and-suspenders check, not a reachable UI state.
      if (!signatureDataUrl) {
        toast.error('A signature is required before submitting.');
        return;
      }

      try {
        const result = await submitDonor.mutateAsync(formValuesToPublicSubmitRequest(getValues(), signatureDataUrl));
        setSubmissionResult(result);
        setPhase('uploading-document');
      } catch (err) {
        const apiError = err as ApiError;
        toast.error(apiError.response?.data?.message ?? 'Something went wrong submitting your form. Please try again.');
      }
      return;
    }

    const fieldsToValidate = STEP_FIELD_PATHS[currentStep] ?? [];
    const valid = fieldsToValidate.length === 0 ? true : await trigger(fieldsToValidate);
    if (valid) goNext();
  }

  if (phase === 'uploading-document' && submissionResult) {
    return (
      <PublicPageShell>
        <PublicCard>
          <h2 className="mb-1 text-[19px] font-extrabold tracking-tight">
            Upload your BBBEE certificate <span className="font-medium text-[#9A9A90]">(optional)</span>
          </h2>
          <DocumentUploadStep
            submissionToken={submissionResult.submissionToken}
            onUploaded={() => setPhase('complete')}
            onSkip={() => setPhase('complete')}
          />
        </PublicCard>
      </PublicPageShell>
    );
  }

  if (phase === 'complete' && submissionResult) {
    return <ConfirmationScreen message={submissionResult.message} referenceNumber={submissionResult.referenceNumber} />;
  }

  const stepIndex = currentStep - 1;

  return (
    <PublicFormLayout
      stepLabels={STEP_LABELS}
      currentStep={currentStep}
      onStepClick={goToStep}
      onBack={goBack}
      onNext={handleNext}
      isFirstStep={isFirst}
      isLastStep={isLast}
      isNextDisabled={submitDonor.isPending}
      nextLabel={isLast && submitDonor.isPending ? 'Submitting…' : undefined}
    >
      <h2 className="mb-1 text-[19px] font-extrabold tracking-tight">{STEP_TITLES[stepIndex]}</h2>
      <p className="mb-6 text-[13px] font-medium text-[#82827A]">{STEP_DESCRIPTIONS[stepIndex]}</p>

      {currentStep === 1 && <CompanyStep register={register} errors={errors} />}
      {currentStep === 2 && (
        <ContactsStep
          register={register}
          control={control}
          errors={errors}
          onToggleMarketingContact={(checked) => setValue('hasMarketingContact', checked)}
          onToggleAccountsContact={(checked) => setValue('hasAccountsContact', checked)}
        />
      )}
      {currentStep === 3 && <AddressStep register={register} errors={errors} />}
      {currentStep === 4 && <DonationsStep register={register} control={control} errors={errors} />}
      {currentStep === 5 && <ComplianceStep register={register} errors={errors} />}
      {currentStep === 6 && (
        <SignatureStep
          onChange={(dataUrl) => {
            setSignatureDataUrl(dataUrl);
            if (dataUrl) setSignatureError(undefined);
          }}
          error={signatureError}
        />
      )}
      {currentStep === 7 && (
        <ReviewStep
          control={control}
          hasSignature={signatureDataUrl !== null}
          confirmChecked={confirmChecked}
          onConfirmChange={setConfirmChecked}
          confirmError={confirmError}
          onEditStep={goToStep}
        />
      )}
    </PublicFormLayout>
  );
}
