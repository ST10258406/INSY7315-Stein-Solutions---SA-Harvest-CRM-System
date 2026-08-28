import type { InputHTMLAttributes } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';

interface FormFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'className'> {
  label: string;
  error?: string;
  fullWidth?: boolean;
  required?: boolean;
  hint?: string;
  registration: UseFormRegisterReturn;
}

export function FormField({ label, error, fullWidth, required, hint, registration, ...inputProps }: FormFieldProps) {
  return (
    <div className={`flex flex-col gap-2 ${fullWidth ? 'sm:col-span-2' : ''}`}>
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
      <input
        id={registration.name}
        {...registration}
        {...inputProps}
        className={`h-11 rounded-full border bg-background px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
          error ? 'border-destructive/70' : 'border-border'
        }`}
      />
      {hint && !error && <span className="pl-3.75 text-[11.5px] font-medium text-muted-foreground">{hint}</span>}
      {error && <span className="pl-3.75 text-xs text-destructive">{error}</span>}
    </div>
  );
}
