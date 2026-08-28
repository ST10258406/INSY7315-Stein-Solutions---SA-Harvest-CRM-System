import type { UseFormRegisterReturn } from 'react-hook-form';
import { ChevronDown } from 'lucide-react';

export interface FormSelectOption {
  value: string;
  label: string;
}

interface FormSelectProps {
  label: string;
  error?: string;
  fullWidth?: boolean;
  required?: boolean;
  registration: UseFormRegisterReturn;
  options: FormSelectOption[];
  placeholder: string;
  isLoading?: boolean;
  disabled?: boolean;
}

export function FormSelect({
  label,
  error,
  fullWidth,
  required,
  registration,
  options,
  placeholder,
  isLoading,
  disabled,
}: FormSelectProps) {
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
      <div className="relative">
        <select
          id={registration.name}
          {...registration}
          disabled={disabled || isLoading}
          className={`h-11 w-full appearance-none rounded-full border bg-field px-3.75 pr-10 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 disabled:opacity-50 ${
            error ? 'border-destructive/70' : 'border-border'
          }`}
        >
          <option value="">{isLoading ? 'Loading…' : placeholder}</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <ChevronDown className="pointer-events-none absolute top-3.5 right-4 h-3.5 w-3.5 text-muted-foreground" />
      </div>
      {error && <span className="pl-3.75 text-xs text-destructive">{error}</span>}
    </div>
  );
}
