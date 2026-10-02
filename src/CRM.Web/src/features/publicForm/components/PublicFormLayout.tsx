import type { ReactNode } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { StepIndicator } from './StepIndicator';
import { PublicPageShell } from './PublicPageShell';
import { PublicCard } from './PublicCard';

interface PublicFormLayoutProps {
  stepLabels: string[];
  currentStep: number;
  onStepClick?: (step: number) => void;
  onBack: () => void;
  onNext: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
  isNextDisabled?: boolean;
  nextLabel?: ReactNode;
  children: ReactNode;
}

/**
 * The multi-step form screen of the public donor flow — steps 1–7 of the
 * approved design, with the step indicator and Next/Back nav. What happens
 * after a successful Submit (document upload, confirmation) lives outside
 * this component but shares its PublicPageShell chrome.
 */
export function PublicFormLayout({
  stepLabels,
  currentStep,
  onStepClick,
  onBack,
  onNext,
  isFirstStep,
  isLastStep,
  isNextDisabled,
  nextLabel,
  children,
}: PublicFormLayoutProps) {
  return (
    <PublicPageShell>
      <StepIndicator labels={stepLabels} currentStep={currentStep} onStepClick={onStepClick} />

      <PublicCard>
        {children}

        <div className="mt-7.5 flex items-center justify-between border-t border-[var(--hair)] pt-5.5">
          <div>
            {!isFirstStep && (
              <Button type="button" variant="secondary" onClick={onBack}>
                <ChevronLeft className="h-3.5 w-3.5" />
                Back
              </Button>
            )}
          </div>
          <div className="flex items-center gap-3.5">
            <span className="text-xs font-semibold text-[var(--muted2)]">
              Step {currentStep} of {stepLabels.length}
            </span>
            <Button type="button" onClick={onNext} disabled={isNextDisabled}>
              {nextLabel ?? (
                <>
                  {isLastStep ? 'Submit' : 'Next'}
                  {!isLastStep && <ChevronRight className="h-3.5 w-3.5" />}
                </>
              )}
            </Button>
          </div>
        </div>
      </PublicCard>
    </PublicPageShell>
  );
}
