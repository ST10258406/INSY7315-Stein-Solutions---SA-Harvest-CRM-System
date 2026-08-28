interface FormCheckboxProps {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  fullWidth?: boolean;
}

export function FormCheckbox({ label, checked, onChange, fullWidth = true }: FormCheckboxProps) {
  return (
    <label className={`flex cursor-pointer items-center gap-2 ${fullWidth ? 'sm:col-span-2' : ''}`}>
      <input type="checkbox" className="sr-only" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      <span
        className={`flex h-[15px] w-[15px] flex-none items-center justify-center rounded border-[1.5px] ${
          checked ? 'border-brand bg-brand' : 'border-[#2B2B23] bg-transparent'
        }`}
      >
        {checked && (
          <svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="#16160F" strokeWidth={3.4}>
            <path d="M5 13l4 4L19 7" />
          </svg>
        )}
      </span>
      <span className="text-sm text-[#F4F4EE]">{label}</span>
    </label>
  );
}
