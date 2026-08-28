export interface FormCheckboxGroupOption {
  value: string;
  label: string;
}

interface FormCheckboxGroupProps {
  label: string;
  error?: string;
  options: FormCheckboxGroupOption[];
  value: string[];
  onChange: (value: string[]) => void;
  isLoading?: boolean;
}

export function FormCheckboxGroup({ label, error, options, value, onChange, isLoading }: FormCheckboxGroupProps) {
  function toggle(optionValue: string) {
    onChange(value.includes(optionValue) ? value.filter((v) => v !== optionValue) : [...value, optionValue]);
  }

  return (
    <div className="flex flex-col gap-1.5 sm:col-span-2">
      <span className="text-xs text-[#6B6B60] uppercase tracking-wide">{label}</span>
      <div className="flex flex-wrap gap-2">
        {isLoading && <span className="text-xs text-[#6B6B60]">Loading…</span>}
        {!isLoading &&
          options.map((option) => {
            const checked = value.includes(option.value);
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => toggle(option.value)}
                aria-pressed={checked}
                className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${
                  checked
                    ? 'border-brand bg-brand/10 text-brand'
                    : 'border-[#2B2B23] text-[#B9B9AE] hover:border-[#F4F4EE]'
                }`}
              >
                {option.label}
              </button>
            );
          })}
      </div>
      {error && <span className="text-xs text-rose-400">{error}</span>}
    </div>
  );
}
