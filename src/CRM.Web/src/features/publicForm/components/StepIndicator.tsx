import { Check } from 'lucide-react';
import { cn } from '@/lib/utils';

interface StepIndicatorProps {
  labels: string[];
  currentStep: number; // 1-indexed
  onStepClick?: (step: number) => void;
}

export function StepIndicator({ labels, currentStep, onStepClick }: StepIndicatorProps) {
  return (
    <div className="mb-3 sm:mb-7">
      <div className="flex items-start" role="list" aria-label="Form steps">
        {labels.map((label, index) => {
          const step = index + 1;
          const isDone = step < currentStep;
          const isCurrent = step === currentStep;

          return (
            <div key={label} className="relative flex min-w-0 flex-1 flex-col items-center gap-1.5" role="listitem">
              {index > 0 && (
                <div
                  aria-hidden
                  className={cn(
                    'absolute top-4 right-1/2 z-0 h-0.5 w-full',
                    step <= currentStep ? 'bg-[#16160F]' : 'bg-[var(--border)]'
                  )}
                />
              )}
              <button
                type="button"
                onClick={() => onStepClick?.(step)}
                aria-current={isCurrent ? 'step' : undefined}
                aria-label={`Step ${step}: ${label}`}
                className={cn(
                  'relative z-10 flex h-8 w-8 items-center justify-center rounded-full text-[12.5px] font-extrabold transition-colors',
                  isCurrent && 'bg-[var(--brand-yellow)] text-[#16160F] shadow-[0_2px_8px_rgba(250,223,1,0.5)]',
                  isDone && !isCurrent && 'bg-[#16160F] text-[var(--brand-yellow)]',
                  !isCurrent && !isDone && 'border-[1.5px] border-[var(--border)] bg-white text-[var(--muted2)]'
                )}
              >
                {isDone && !isCurrent ? <Check className="h-3.5 w-3.5 stroke-[3]" /> : step}
              </button>
              {/* Per-step labels crowd and overlap once there are 7 of them at
                  phone width — shown from sm up only; the current step's name
                  still surfaces below on mobile. */}
              <span
                className={cn(
                  'hidden truncate text-[10.5px] font-semibold whitespace-nowrap sm:block',
                  isCurrent ? 'font-extrabold text-[#16160F]' : 'text-[var(--muted2)]'
                )}
              >
                {label}
              </span>
            </div>
          );
        })}
      </div>
      <p className="mt-2 text-center text-[11px] font-bold text-[#16160F] sm:hidden">{labels[currentStep - 1]}</p>
    </div>
  );
}
