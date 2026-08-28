interface FormCheckboxProps {
  label: string;
  description?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  fullWidth?: boolean;
}

export function FormCheckbox({ label, description, checked, onChange, fullWidth = true }: FormCheckboxProps) {
  return (
    <div className={`flex items-start justify-between gap-5 ${fullWidth ? 'sm:col-span-2' : ''}`}>
      <div>
        <span className="text-xs font-bold text-foreground">{label}</span>
        {description && <p className="m-0 mt-1 text-[11.5px] font-medium text-muted-foreground">{description}</p>}
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-label={label}
        onClick={() => onChange(!checked)}
        className={`relative h-7 w-12.5 shrink-0 cursor-pointer rounded-full border transition-colors ${
          checked ? 'border-brand bg-brand' : 'border-border bg-muted'
        }`}
      >
        <span
          className={`absolute top-[3px] h-5 w-5 rounded-full bg-card shadow-sm transition-all ${
            checked ? 'left-[23px]' : 'left-[3px]'
          }`}
        />
      </button>
    </div>
  );
}
