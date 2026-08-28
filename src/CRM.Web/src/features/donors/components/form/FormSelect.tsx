import type { UseFormRegisterReturn } from 'react-hook-form';

export interface FormSelectOption {
  value: string;
  label: string;
}

interface FormSelectProps {
  label: string;
  error?: string;
  fullWidth?: boolean;
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
  registration,
  options,
  placeholder,
  isLoading,
  disabled,
}: FormSelectProps) {
  return (
    <label className={`flex flex-col gap-1.5 ${fullWidth ? 'sm:col-span-2' : ''}`}>
      <span className="text-xs text-[#6B6B60] uppercase tracking-wide">{label}</span>
      <select
        {...registration}
        disabled={disabled || isLoading}
        className={`h-9 rounded-lg border bg-[#0F0F0C] px-3 text-sm text-[#F4F4EE] outline-none focus-visible:border-[#F4F4EE] disabled:opacity-50 ${
          error ? 'border-rose-500/70' : 'border-[#2B2B23]'
        }`}
      >
        <option value="">{isLoading ? 'Loading…' : placeholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && <span className="text-xs text-rose-400">{error}</span>}
    </label>
  );
}
