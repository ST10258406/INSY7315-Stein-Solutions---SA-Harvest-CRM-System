import { Check } from 'lucide-react';

export interface FormCheckboxGroupOption {
  value: string;
  label: string;
}

interface FormCheckboxGroupProps {
  label: string;
  error?: string;
  required?: boolean;
  options: FormCheckboxGroupOption[];
  value: string[];
  onChange: (value: string[]) => void;
  isLoading?: boolean;
}

export function FormCheckboxGroup({ label, error, required, options, value, onChange, isLoading }: FormCheckboxGroupProps) {
  function toggle(optionValue: string) {
    onChange(value.includes(optionValue) ? value.filter((v) => v !== optionValue) : [...value, optionValue]);
  }

  return (
    <div className="flex flex-col gap-2.5 sm:col-span-2">
      <span className="flex items-center gap-1 text-xs font-bold text-foreground">
        <span>{label}</span>
        {required && <span className="text-destructive">*</span>}
      </span>
      <div className="flex flex-wrap gap-2">
        {isLoading && <span className="text-xs font-medium text-muted-foreground">Loading…</span>}
        {!isLoading &&
          options.map((option) => {
            const checked = value.includes(option.value);
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => toggle(option.value)}
                aria-pressed={checked}
                className={`inline-flex h-9 items-center gap-1.75 rounded-full border px-3.75 text-[12.5px] font-bold transition-colors ${
                  checked
                    ? 'border-brand bg-brand text-primary-foreground'
                    : 'border-border bg-background text-foreground hover:border-foreground/40'
                }`}
              >
                {checked && <Check className="h-3 w-3 stroke-[3.2]" />}
                <span>{option.label}</span>
              </button>
            );
          })}
      </div>
      {error && <span className="text-xs text-destructive">{error}</span>}
    </div>
  );
}
