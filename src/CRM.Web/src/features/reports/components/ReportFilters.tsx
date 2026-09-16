import { useMemo } from 'react';
import { CalendarBlank } from '@phosphor-icons/react';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Calendar } from '@/components/ui/calendar';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useRelationshipManagers } from '@/features/lookups';
import { formatDate } from '@/features/donors/lib/donorFormatters';
import { fromYmd, toYmd } from '../lib/date';
import type { DonorsContactedFilters } from '../types';

const ALL_MANAGERS_VALUE = 'all';

interface ReportFiltersProps {
  value: DonorsContactedFilters;
  onChange: (value: DonorsContactedFilters) => void;
}

/**
 * Filter bar for the Donors Contacted report only — the other three report charts are always
 * all-time and unfiltered, so this is deliberately not a page-wide "ReportFilters applies to
 * everything" component. Render it directly above DonorsContactedChart and nowhere else.
 *
 * startDate/endDate are both always present on `value` (ReportsPage seeds it with "this
 * calendar month"), and the min/max bounds below keep it that way — but Handle*Select still
 * guards against firing onChange with an incomplete range, since that's the actual acceptance
 * criterion, not just a side effect of the bounds.
 */
export function ReportFilters({ value, onChange }: ReportFiltersProps) {
  const managers = useRelationshipManagers();
  const startDate = fromYmd(value.startDate);
  const endDate = fromYmd(value.endDate);

  // Base UI's <Select.Value> shows the raw value string unless Root's `items` gives it a
  // value→label lookup — without this it would render the manager's GUID (or "all") verbatim
  // instead of their name.
  const managerItems = useMemo(() => {
    const items: Record<string, string> = { [ALL_MANAGERS_VALUE]: 'All managers' };
    for (const manager of managers.data ?? []) items[manager.id] = manager.fullName;
    return items;
  }, [managers.data]);

  function handleStartSelect(date: Date | undefined) {
    if (!date || !endDate || date > endDate) return;
    onChange({ ...value, startDate: toYmd(date) });
  }

  function handleEndSelect(date: Date | undefined) {
    if (!date || !startDate || date < startDate) return;
    onChange({ ...value, endDate: toYmd(date) });
  }

  function handleManagerChange(managerId: string) {
    onChange({ ...value, relationshipManagerId: managerId === ALL_MANAGERS_VALUE ? undefined : managerId });
  }

  return (
    <section className="mb-4 flex flex-wrap items-end gap-3 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4">
      <div className="flex flex-col gap-1.5">
        <Label className="text-[11.5px] font-semibold text-[var(--muted-c)]">Start date</Label>
        <Popover>
          <PopoverTrigger
            render={
              <Button type="button" variant="secondary" size="sm" className="justify-start gap-1.5">
                <CalendarBlank className="h-3.5 w-3.5" />
                {formatDate(value.startDate)}
              </Button>
            }
          />
          <PopoverContent className="w-auto p-0">
            <Calendar mode="single" selected={startDate} onSelect={handleStartSelect} disabled={{ after: endDate }} />
          </PopoverContent>
        </Popover>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-[11.5px] font-semibold text-[var(--muted-c)]">End date</Label>
        <Popover>
          <PopoverTrigger
            render={
              <Button type="button" variant="secondary" size="sm" className="justify-start gap-1.5">
                <CalendarBlank className="h-3.5 w-3.5" />
                {formatDate(value.endDate)}
              </Button>
            }
          />
          <PopoverContent className="w-auto p-0">
            <Calendar mode="single" selected={endDate} onSelect={handleEndSelect} disabled={{ before: startDate }} />
          </PopoverContent>
        </Popover>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-[11.5px] font-semibold text-[var(--muted-c)]">Relationship manager</Label>
        <Select
          items={managerItems}
          value={value.relationshipManagerId ?? ALL_MANAGERS_VALUE}
          onValueChange={handleManagerChange}
        >
          <SelectTrigger size="sm" className="min-w-[180px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_MANAGERS_VALUE}>All managers</SelectItem>
            {managers.data?.map((manager) => (
              <SelectItem key={manager.id} value={manager.id}>
                {manager.fullName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </section>
  );
}
