import { PublicFormLayout } from './components/PublicFormLayout';
import { usePublicFormSteps } from './hooks/usePublicFormSteps';

// Step grouping/order matches the approved design (SA Harvest Donor
// Onboarding mockup). Field content lands in a later issue — this is the
// structural scaffold only.
const STEPS = [
  { label: 'Company Info', title: 'Company Information', description: "Tell us about your company." },
  { label: 'Contacts', title: 'Contacts', description: 'Who should we speak to at your company?' },
  { label: 'Address', title: 'Registered Address', description: "Your company's registered address." },
  { label: 'Donations', title: 'Donation Information', description: 'Help us plan collections and match your donations to need.' },
  { label: 'Compliance', title: 'Compliance', description: 'All fields on this step are optional.' },
  { label: 'Signature', title: 'Signature', description: 'Please sign to confirm the information provided is accurate.' },
  { label: 'Review', title: 'Review', description: 'Please check your details before submitting.' },
];

export default function PublicFormPage() {
  const { currentStep, isFirst, isLast, goNext, goBack, goToStep } = usePublicFormSteps(STEPS.length);
  const step = STEPS[currentStep - 1];

  return (
    <PublicFormLayout
      stepLabels={STEPS.map((s) => s.label)}
      currentStep={currentStep}
      onStepClick={goToStep}
      onBack={goBack}
      onNext={goNext}
      isFirstStep={isFirst}
      isLastStep={isLast}
    >
      <h2 className="mb-1 text-[19px] font-extrabold tracking-tight">{step.title}</h2>
      <p className="mb-6 text-[13px] font-medium text-[var(--muted-c)]">{step.description}</p>
      <div className="rounded-xl border border-dashed border-[var(--border)] bg-white/60 p-10 text-center text-sm font-medium text-[var(--muted2)]">
        Form fields for this step will go here.
      </div>
    </PublicFormLayout>
  );
}
