import { useState } from 'react';

// In-memory only, by design — a refresh mid-form loses progress for now.
// See PublicFormPage.tsx for the sessionStorage-draft follow-up note.
export function usePublicFormSteps(totalSteps: number) {
  const [currentStep, setCurrentStep] = useState(1);

  const isFirst = currentStep === 1;
  const isLast = currentStep === totalSteps;

  function goNext() {
    setCurrentStep((step) => Math.min(totalSteps, step + 1));
  }

  function goBack() {
    setCurrentStep((step) => Math.max(1, step - 1));
  }

  function goToStep(step: number) {
    setCurrentStep(Math.min(totalSteps, Math.max(1, step)));
  }

  return { currentStep, totalSteps, isFirst, isLast, goNext, goBack, goToStep };
}
