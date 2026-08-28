import type { InputHTMLAttributes } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';

interface FormFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'className'> {
  label: string;
  error?: string;
  fullWidth?: boolean;
  registration: UseFormRegisterReturn;
}

export function FormField({ label, error, fullWidth, registration, ...inputProps }: FormFieldProps) {
  return (
    <label className={`flex flex-col gap-1.5 ${fullWidth ? 'sm:col-span-2' : ''}`}>
      <span className="text-xs text-[#6B6B60] uppercase tracking-wide">{label}</span>
      <input
        {...registration}
        {...inputProps}
        className={`h-9 rounded-lg border bg-[#0F0F0C] px-3 text-sm text-[#F4F4EE] outline-none placeholder:text-[#6B6B60] focus-visible:border-[#F4F4EE] ${
          error ? 'border-rose-500/70' : 'border-[#2B2B23]'
        }`}
      />
      {error && <span className="text-xs text-rose-400">{error}</span>}
    </label>
  );
}
