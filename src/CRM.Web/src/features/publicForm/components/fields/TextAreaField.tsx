import type { TextareaHTMLAttributes } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';
import { cn } from '@/lib/utils';

interface TextAreaFieldProps extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'className'> {
  label: string;
  error?: string;
  required?: boolean;
  optional?: boolean;
  hint?: string;
  fullWidth?: boolean;
  registration: UseFormRegisterReturn;
}

export function TextAreaField({
  label,
  error,
  required,
  optional,
  hint,
  fullWidth,
  registration,
  rows = 3,
  ...textareaProps
}: TextAreaFieldProps) {
  return (
    <label className={cn('flex flex-col gap-1.5', fullWidth && 'sm:col-span-2')}>
      <span className="text-[12.5px] font-bold text-[#16160F]">
        {label} {required && <span className="text-[#D4373A]">*</span>}
        {!required && optional && <span className="font-medium text-[#9A9A90]">(optional)</span>}
      </span>
      <textarea
        {...registration}
        {...textareaProps}
        rows={rows}
        className={cn(
          'w-full resize-y rounded-[10px] border-[1.5px] bg-white px-3.5 py-3 text-[13.5px] leading-relaxed text-[#16160F] outline-none placeholder:text-[#B4B4AA] focus:border-[#16160F]',
          error ? 'border-[#D4373A] bg-[#FDF6F6]' : 'border-[#E4E4DE]'
        )}
      />
      {hint && !error && <span className="text-[11.5px] font-medium text-[#82827A]">{hint}</span>}
      {error && <span className="text-[11.5px] font-semibold text-[#D4373A]">{error}</span>}
    </label>
  );
}
