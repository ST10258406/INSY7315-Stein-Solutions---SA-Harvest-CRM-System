import type { TextareaHTMLAttributes } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';

interface FormTextAreaProps extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'className'> {
  label: string;
  error?: string;
  required?: boolean;
  hint?: string;
  registration: UseFormRegisterReturn;
}

export function FormTextArea({ label, error, required, hint, registration, rows = 3, ...textareaProps }: FormTextAreaProps) {
  return (
    <div className="flex flex-col gap-2 sm:col-span-2">
      <span className="flex items-center gap-1">
        <label htmlFor={registration.name} className="text-xs font-bold text-foreground">
          {label}
        </label>
        {required && (
          <span aria-hidden="true" className="text-xs font-bold text-destructive">
            *
          </span>
        )}
      </span>
      <textarea
        id={registration.name}
        {...registration}
        {...textareaProps}
        rows={rows}
        className={`resize-y rounded-2xl border bg-field px-3.75 py-3.5 text-[13.5px] leading-relaxed font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
          error ? 'border-destructive/70' : 'border-border'
        }`}
      />
      {hint && !error && <span className="pl-3.75 text-[11.5px] font-medium text-muted-foreground">{hint}</span>}
      {error && <span className="pl-3.75 text-xs text-destructive">{error}</span>}
    </div>
  );
}
