interface ChipOption {
  value: string;
  label: string;
}

interface ChipMultiSelectProps {
  label: string;
  required?: boolean;
  error?: string;
  options: ChipOption[];
  value: string[];
  onChange: (value: string[]) => void;
  isLoading?: boolean;
}

export function ChipMultiSelect({ label, required, error, options, value, onChange, isLoading }: ChipMultiSelectProps) {
  function toggle(optionValue: string) {
    onChange(value.includes(optionValue) ? value.filter((v) => v !== optionValue) : [...value, optionValue]);
  }

  return (
    <div className="flex flex-col gap-2">
      <span className="text-[12.5px] font-bold text-[#16160F]">
        {label} {required && <span className="text-[#D4373A]">*</span>}
      </span>
      <div className="flex flex-wrap gap-2">
        {isLoading && <span className="text-[12.5px] font-medium text-[#82827A]">Loading…</span>}
        {!isLoading &&
          options.map((option) => {
            const selected = value.includes(option.value);
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => toggle(option.value)}
                aria-pressed={selected}
                className={
                  selected
                    ? 'h-9 cursor-pointer rounded-full border-none bg-[#16160F] px-4 text-[12.5px] font-bold text-[#FADF01]'
                    : 'h-9 cursor-pointer rounded-full border-[1.5px] border-[#E4E4DE] bg-white px-4 text-[12.5px] font-bold text-[#82827A]'
                }
              >
                {option.label}
              </button>
            );
          })}
      </div>
      {error && <span className="text-[11.5px] font-semibold text-[#D4373A]">{error}</span>}
    </div>
  );
}
