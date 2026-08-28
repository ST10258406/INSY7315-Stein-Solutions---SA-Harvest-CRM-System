import type { TextareaHTMLAttributes } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';

interface FormTextAreaProps extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'className'> {
  label: string;
  error?: string;
  registration: UseFormRegisterReturn;
}

export function FormTextArea({ label, error, registration, rows = 3, ...textareaProps }: FormTextAreaProps) {
  return (
    <label className="flex flex-col gap-1.5 sm:col-span-2">
      <span className="text-xs text-[#6B6B60] uppercase tracking-wide">{label}</span>
      <textarea
        {...registration}
        {...textareaProps}
        rows={rows}
        className={`rounded-lg border bg-[#0F0F0C] px-3 py-2 text-sm text-[#F4F4EE] outline-none placeholder:text-[#6B6B60] focus-visible:border-[#F4F4EE] ${
          error ? 'border-rose-500/70' : 'border-[#2B2B23]'
        }`}
      />
      {error && <span className="text-xs text-rose-400">{error}</span>}
    </label>
  );
}
