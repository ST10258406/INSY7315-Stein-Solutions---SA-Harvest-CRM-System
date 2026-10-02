import type { LucideIcon } from 'lucide-react';
import { ChevronDown, FilterX } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverDescription, PopoverTitle, PopoverTrigger } from '@/components/ui/popover';

export interface FilterOption {
  value: string;
  label: string;
}

export interface FilterField {
  key: string;
  label: string;
  /** Label of the "no filter" option, e.g. "All regions". */
  allLabel: string;
  options: FilterOption[];
  isLoading?: boolean;
}

interface DashboardFilterPopoverProps {
  title: string;
  description: string;
  fields: FilterField[];
  /** Current value per field key — `undefined` means "All". */
  values: Record<string, string | undefined>;
  onChange: (key: string, value: string | undefined) => void;
  onClear: () => void;
  icon: LucideIcon;
  /** Text shown on the trigger; omit for an icon-only trigger. */
  triggerLabel?: string;
}

const SELECT_CLASSES =
  'h-9 w-full appearance-none rounded-full border bg-[var(--card)] pr-8 pl-3.5 text-[12.5px] font-semibold text-[var(--ink)] outline-none transition-colors focus-visible:border-[var(--ink)] disabled:opacity-50';

/**
 * Popover of "All / one value" selects shared by the dashboard's page-level
 * Filters button and the Donor activity section's filter. Purely presentational:
 * state lives in DashboardPage so both filters feed the same widgets.
 */
export function DashboardFilterPopover({
  title,
  description,
  fields,
  values,
  onChange,
  onClear,
  icon: Icon,
  triggerLabel,
}: DashboardFilterPopoverProps) {
  const activeCount = fields.filter((field) => values[field.key] !== undefined).length;
  const ariaLabel = activeCount > 0 ? `${title} (${activeCount} active)` : title;

  return (
    <Popover>
      <PopoverTrigger
        render={
          <Button
            type="button"
            variant={activeCount > 0 ? 'dark' : 'secondary'}
            size={triggerLabel ? 'sm' : 'icon'}
            aria-label={ariaLabel}
            title={title}
          >
            <Icon className={triggerLabel ? 'w-3.75 h-3.75' : 'w-4 h-4'} />
            {triggerLabel && <span>{triggerLabel}</span>}
            {triggerLabel && activeCount > 0 && (
              <span className="ml-0.5 rounded-full bg-brand px-1.5 text-[10.5px] font-bold text-primary-foreground">
                {activeCount}
              </span>
            )}
          </Button>
        }
      />
      <PopoverContent align="end" className="w-72 gap-3 p-4">
        <div>
          <PopoverTitle className="m-0 text-sm font-bold text-[var(--ink)]">{title}</PopoverTitle>
          <PopoverDescription className="m-0 mt-0.5 text-[11.5px] font-medium text-[var(--muted-c)]">
            {description}
          </PopoverDescription>
        </div>

        {fields.map((field) => {
          const id = `dashboard-filter-${field.key}`;
          const value = values[field.key];
          return (
            <div key={field.key} className="flex flex-col gap-1.5">
              <label htmlFor={id} className="text-[11.5px] font-semibold text-[var(--muted-c)]">
                {field.label}
              </label>
              <div className="relative">
                <select
                  id={id}
                  value={value ?? ''}
                  disabled={field.isLoading}
                  onChange={(e) => onChange(field.key, e.target.value || undefined)}
                  className={`${SELECT_CLASSES} ${value ? 'border-[var(--ink)]' : 'border-[var(--border)]'}`}
                >
                  <option value="">{field.allLabel}</option>
                  {field.options.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute top-1/2 right-3 h-3.5 w-3.5 -translate-y-1/2 text-[var(--icon)]" />
              </div>
            </div>
          );
        })}

        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onClear}
          disabled={activeCount === 0}
          className="self-start font-bold text-[var(--muted-c)] hover:text-[var(--ink)]"
        >
          <FilterX className="w-3.5 h-3.5" />
          Clear filters
        </Button>
      </PopoverContent>
    </Popover>
  );
}
