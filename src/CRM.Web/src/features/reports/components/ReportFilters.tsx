import { useMemo } from 'react';
import { DatePicker } from '@/components/common/DatePicker';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useRelationshipManagers } from '@/features/lookups';
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

  // Base UI's <Select.Value> shows the raw value string unless Root's `items` gives it a
  // value→label lookup — without this it would render the manager's GUID (or "all") verbatim
  // instead of their name.
  const managerItems = useMemo(() => {
    const items: Record<string, string> = { [ALL_MANAGERS_VALUE]: 'All managers' };
    for (const manager of managers.data ?? []) items[manager.id] = manager.fullName;
    return items;
  }, [managers.data]);

  function handleStartSelect(date: string) {
    if (!date || date > value.endDate) return;
    onChange({ ...value, startDate: date });
  }

  function handleEndSelect(date: string) {
    if (!date || date < value.startDate) return;
    onChange({ ...value, endDate: date });
  }

  function handleManagerChange(managerId: string | null) {
    onChange({
      ...value,
      relationshipManagerId: !managerId || managerId === ALL_MANAGERS_VALUE ? undefined : managerId,
    });
  }

  return (
    <section className="mb-4 flex flex-wrap items-end gap-3 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4">
      <div className="flex flex-col gap-1.5">
        <Label className="text-[11.5px] font-semibold text-[var(--muted-c)]">Start date</Label>
        <DatePicker value={value.startDate} max={value.endDate} onChange={handleStartSelect} aria-label="Start date" />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-[11.5px] font-semibold text-[var(--muted-c)]">End date</Label>
        <DatePicker value={value.endDate} min={value.startDate} onChange={handleEndSelect} aria-label="End date" />
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
