import type { SelectHTMLAttributes } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';
import { cn } from '@/lib/utils';

interface SelectOption {
  value: string;
  label: string;
}

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'className'> {
  label: string;
  error?: string;
  required?: boolean;
  optional?: boolean;
  placeholder: string;
  options: SelectOption[];
  isLoading?: boolean;
  fullWidth?: boolean;
  registration: UseFormRegisterReturn;
}

export function SelectField({
  label,
  error,
  required,
  optional,
  placeholder,
  options,
  isLoading,
  fullWidth,
  registration,
  ...selectProps
}: SelectFieldProps) {
  return (
    <label className={cn('flex flex-col gap-1.5', fullWidth && 'sm:col-span-2')}>
      <span className="text-[12.5px] font-bold text-[#16160F]">
        {label} {required && <span className="text-[#D4373A]">*</span>}
        {!required && optional && <span className="font-medium text-[#9A9A90]">(optional)</span>}
      </span>
      <select
        {...registration}
        {...selectProps}
        disabled={isLoading || selectProps.disabled}
        className={cn(
          'h-[46px] w-full cursor-pointer rounded-[10px] border-[1.5px] bg-white px-3 text-[13.5px] text-[#16160F] outline-none disabled:cursor-wait disabled:opacity-60',
          error ? 'border-[#D4373A] bg-[#FDF6F6]' : 'border-[#E4E4DE]'
        )}
      >
        <option value="">{isLoading ? 'Loading…' : placeholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && <span className="text-[11.5px] font-semibold text-[#D4373A]">{error}</span>}
    </label>
  );
}
