import type { ReactNode } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import logoImg from '@/assets/sa-harvest-logo.png';
import { Button } from '@/components/ui/button';
import { StepIndicator } from './StepIndicator';

interface PublicFormLayoutProps {
  stepLabels: string[];
  currentStep: number;
  onStepClick?: (step: number) => void;
  onBack: () => void;
  onNext: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
  nextLabel?: ReactNode;
  children: ReactNode;
}

/**
 * Standalone shell for the public donor onboarding form — deliberately
 * outside AppLayout (no sidebar/header/user menu) since this is reached with
 * zero authentication. Styled per the approved design (light, warm,
 * donor-facing) rather than the internal CRM's dark admin aesthetic.
 */
export function PublicFormLayout({
  stepLabels,
  currentStep,
  onStepClick,
  onBack,
  onNext,
  isFirstStep,
  isLastStep,
  nextLabel,
  children,
}: PublicFormLayoutProps) {
  return (
    <div className="flex min-h-screen flex-col bg-white text-[#16160F]" style={{ fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif" }}>
      <header className="flex flex-col items-center gap-3.5 px-6 pt-11 pb-2.5 text-center">
        <img src={logoImg} alt="" className="h-[78px] w-[78px] rounded-[18px] object-cover" />
        <div>
          <div className="text-2xl font-extrabold tracking-tight">S.A. Harvest</div>
          <div className="mt-0.5 text-[10.5px] font-bold tracking-[2px] text-[var(--muted-c)]">DONOR ONBOARDING</div>
        </div>
        <p className="mx-auto max-w-[460px] text-[14.5px] leading-relaxed font-medium text-[var(--muted-c)]">
          Partner with us to fight food insecurity in South Africa.
        </p>
      </header>

      <main className="mx-auto w-full max-w-[860px] flex-1 px-6 pt-7 pb-15">
        <StepIndicator labels={stepLabels} currentStep={currentStep} onStepClick={onStepClick} />

        <div className="rounded-2xl border border-[var(--border)] bg-[#F6F6F3] p-6 pt-7.5 pb-6.5 shadow-[0_1px_3px_var(--shadow)] sm:p-8 sm:pt-7.5">
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
              <Button type="button" onClick={onNext}>
                {nextLabel ?? (
                  <>
                    {isLastStep ? 'Submit' : 'Next'}
                    {!isLastStep && <ChevronRight className="h-3.5 w-3.5" />}
                  </>
                )}
              </Button>
            </div>
          </div>
        </div>
      </main>

      <footer className="px-6 pt-5.5 pb-8.5 text-center">
        <p className="text-[11.5px] font-medium text-[var(--muted2)]">
          S.A. Harvest NPC · Rescuing food, fighting hunger · Your information is protected under POPIA.
        </p>
      </footer>
    </div>
  );
}
