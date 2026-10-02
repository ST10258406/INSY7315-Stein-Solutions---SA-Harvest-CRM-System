import { useState } from 'react';
import { CalendarBlank } from '@phosphor-icons/react';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { fromYmd, toYmd } from '@/lib/date';
import { cn } from '@/lib/utils';

const DISPLAY_FORMAT: Intl.DateTimeFormatOptions = { day: '2-digit', month: 'short', year: 'numeric' };

/** Trigger looks — `button` matches the Reports filters, `field` matches form inputs in dialogs,
 * `pill` matches the donor list filter bar. The popup calendar is identical for all three. */
const TRIGGER_VARIANTS = {
  button: '',
  field:
    'h-11 w-full justify-start rounded-2xl border-border bg-field px-3.75 text-[13.5px] font-medium shadow-none hover:bg-field',
  pill: 'h-[42px] rounded-full px-3.5 text-[12.5px] font-semibold',
} as const;

interface DatePickerProps {
  /** yyyy-MM-dd, or '' / undefined for no date. */
  value: string | undefined;
  onChange: (value: string) => void;
  /** Earliest selectable day, yyyy-MM-dd (inclusive). */
  min?: string;
  /** Latest selectable day, yyyy-MM-dd (inclusive). */
  max?: string;
  placeholder?: string;
  /** Shows a "Clear" action that sets the value back to ''. For optional dates only. */
  clearable?: boolean;
  variant?: keyof typeof TRIGGER_VARIANTS;
  /** Highlights the trigger as active (used by the filter-bar pill when a filter is set). */
  active?: boolean;
  invalid?: boolean;
  id?: string;
  'aria-label'?: string;
  className?: string;
}

/**
 * The one date picker used across the app: a button that opens the same popover calendar as the
 * Reports filters. Works on yyyy-MM-dd strings in local time (see @/lib/date), so it drops into
 * React Hook Form fields and URL filters that already store dates that way.
 */
export function DatePicker({
  value,
  onChange,
  min,
  max,
  placeholder = 'Pick a date',
  clearable = false,
  variant = 'button',
  active = false,
  invalid = false,
  id,
  'aria-label': ariaLabel,
  className,
}: DatePickerProps) {
  const [open, setOpen] = useState(false);
  const selected = value ? fromYmd(value) : undefined;
  const minDate = min ? fromYmd(min) : undefined;
  const maxDate = max ? fromYmd(max) : undefined;

  const disabled = [
    ...(minDate ? [{ before: minDate }] : []),
    ...(maxDate ? [{ after: maxDate }] : []),
  ];

  function handleSelect(date: Date | undefined) {
    if (!date) return;
    onChange(toYmd(date));
    setOpen(false);
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={
          <Button
            id={id}
            type="button"
            variant="secondary"
            size="sm"
            aria-label={ariaLabel}
            aria-invalid={invalid || undefined}
            className={cn(
              'justify-start gap-1.5',
              TRIGGER_VARIANTS[variant],
              active && 'border-brand bg-brand text-primary-foreground hover:bg-brand',
              invalid && 'border-destructive/70',
              !selected && !active && 'text-muted-foreground',
              className,
            )}
          >
            <CalendarBlank className="h-3.5 w-3.5 shrink-0" />
            <span className="truncate">{selected ? selected.toLocaleDateString('en-ZA', DISPLAY_FORMAT) : placeholder}</span>
          </Button>
        }
      />
      <PopoverContent className="w-auto p-0">
        <Calendar
          mode="single"
          selected={selected}
          onSelect={handleSelect}
          defaultMonth={selected ?? minDate}
          disabled={disabled}
        />
        {clearable && selected && (
          <div className="border-t border-[var(--border)] px-2 pb-2 pt-1.5">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="w-full font-bold text-[var(--muted-c)] hover:text-[var(--ink)]"
              onClick={() => {
                onChange('');
                setOpen(false);
              }}
            >
              Clear date
            </Button>
          </div>
        )}
      </PopoverContent>
    </Popover>
  );
}
