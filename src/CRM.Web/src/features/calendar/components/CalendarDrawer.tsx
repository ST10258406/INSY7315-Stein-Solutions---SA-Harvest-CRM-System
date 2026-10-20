import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { labelDayButton } from 'react-day-picker';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Calendar } from '@/components/ui/calendar';
import { fromYmd, toYmd } from '@/lib/date';
import { paths } from '@/routes/paths';
import { useAuthStore } from '@/store/authStore';
import { useMyTasks } from '@/features/tasks/hooks';
import { donorKeys, fetchDonors } from '@/features/donors/hooks';
import { DonorStatusBadge } from '@/features/donors/components/DonorStatusBadge';
import { addDaysYmd, monthBounds, sastYmd } from '../lib/sast';

// Matches the ProcurementOrAbove policy on GET /api/v1/donors (cosmetic; the API enforces it).
const DONOR_ROLES = ['Procurement', 'Admin', 'SuperAdmin'];
// The API's page-size cap. A month with more is flagged rather than silently cut.
const MAX_ITEMS = 100;

interface CalendarDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

interface CalendarItem {
  key: string;
  day: string;
  kind: 'task' | 'followUp';
  title: string;
  subtitle?: string;
  status?: string;
  donorId: string;
}

/** Issue 104: the caller's open tasks and their donors' follow-ups, by SAST day. */
export function CalendarDrawer({ open, onOpenChange }: CalendarDrawerProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[440px]">
        <SheetHeader>
          <SheetTitle>Calendar</SheetTitle>
          <SheetDescription>Your open tasks and your donors' follow-ups.</SheetDescription>
        </SheetHeader>
        {/* Mounted only while open, so closing the drawer doesn't keep fetching. */}
        {open && <CalendarBody onNavigate={() => onOpenChange(false)} />}
      </SheetContent>
    </Sheet>
  );
}

function CalendarBody({ onNavigate }: { onNavigate: () => void }) {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const canSeeDonors = (user?.roles ?? []).some((role) => DONOR_ROLES.includes(role));

  const today = sastYmd(new Date());
  const [month, setMonth] = useState(monthBounds(today).first);
  const [selected, setSelected] = useState(today);
  const { first, last } = monthBounds(month);

  // Pad by a day each side: the API compares UTC days, but an item at 00:30 SAST on the 1st is
  // 22:30 UTC the day before. Items outside the month are dropped below, after SAST grouping.
  const from = addDaysYmd(first, -1);
  const to = addDaysYmd(last, 1);

  const tasks = useMyTasks({ isCompleted: false, dueAfter: from, dueBefore: to, pageSize: MAX_ITEMS });
  const donorFilters = { relationshipManagerId: user?.id, followUpAfter: from, followUpBefore: to, pageSize: MAX_ITEMS };
  const followUps = useQuery({
    queryKey: donorKeys.list(donorFilters),
    queryFn: () => fetchDonors(donorFilters),
    enabled: canSeeDonors && !!user,
  });

  const items: CalendarItem[] = [
    ...(tasks.data?.data ?? []).map((t) => ({
      key: `task-${t.id}`,
      day: sastYmd(t.dueDate),
      kind: 'task' as const,
      title: t.title,
      subtitle: t.donor.companyName,
      donorId: t.donor.id,
    })),
    ...(canSeeDonors ? (followUps.data?.data ?? []) : [])
      .filter((d) => d.followUpDate)
      .map((d) => ({
        key: `followup-${d.id}`,
        day: sastYmd(d.followUpDate!),
        kind: 'followUp' as const,
        title: d.companyName,
        status: d.status,
        donorId: d.id,
      })),
  ].filter((item) => item.day >= first && item.day <= last);

  const byDay = new Map<string, CalendarItem[]>();
  for (const item of items) byDay.set(item.day, [...(byDay.get(item.day) ?? []), item]);
  const isOverdueDay = (day: string) => day < today && (byDay.get(day)?.length ?? 0) > 0;

  const truncated =
    (tasks.data?.pagination.totalCount ?? 0) > (tasks.data?.data.length ?? 0) ||
    (canSeeDonors && (followUps.data?.pagination.totalCount ?? 0) > (followUps.data?.data.length ?? 0));
  const isLoading = tasks.isLoading || (canSeeDonors && followUps.isLoading);
  const isError = tasks.isError || (canSeeDonors && followUps.isError);
  const dayItems = byDay.get(selected) ?? [];

  const open = (item: CalendarItem) => {
    onNavigate();
    navigate(paths.donorDetail(item.donorId));
  };

  return (
    <div className="min-h-0 flex-1 overflow-y-auto px-6.5 py-4">
      <Calendar
        mode="single"
        required
        month={fromYmd(month)}
        onMonthChange={(date) => setMonth(monthBounds(toYmd(date)).first)}
        selected={fromYmd(selected)}
        onSelect={(date) => date && setSelected(toYmd(date))}
        modifiers={{
          hasItems: (date) => byDay.has(toYmd(date)),
          overdue: (date) => isOverdueDay(toYmd(date)),
        }}
        modifiersClassNames={{
          hasItems: 'font-extrabold underline decoration-2 underline-offset-4',
          overdue: 'text-destructive',
        }}
        labels={{
          labelDayButton: (date, modifiers, options, dateLib) => {
            const day = toYmd(date);
            const count = byDay.get(day)?.length ?? 0;
            const base = labelDayButton(date, modifiers, options, dateLib);
            if (count === 0) return base;
            const overdue = isOverdueDay(day) ? `, ${count} overdue` : '';
            return `${base}, ${count} ${count === 1 ? 'item' : 'items'}${overdue}`;
          },
        }}
        className="mx-auto"
      />

      <div aria-live="polite" className="mt-2 text-[12.5px] text-muted-foreground">
        {isLoading && 'Loading…'}
        {isError && <span className="text-destructive">Couldn't load the calendar. Try again later.</span>}
        {!isLoading && !isError && items.length === 0 && 'Nothing due this month.'}
        {truncated && <span className="block">Showing the first {MAX_ITEMS} items.</span>}
      </div>

      <h3 className="mb-2 mt-4 text-[12.5px] font-bold text-foreground">
        {fromYmd(selected).toLocaleDateString('en-ZA', { weekday: 'long', day: 'numeric', month: 'long' })}
      </h3>
      {dayItems.length === 0 ? (
        <p className="m-0 text-[12.5px] text-muted-foreground">Nothing on this day.</p>
      ) : (
        <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
          {dayItems.map((item) => (
            <li key={item.key}>
              <button
                type="button"
                onClick={() => open(item)}
                className="flex w-full items-center justify-between gap-3 rounded-2xl border border-border bg-card px-3.5 py-2.5 text-left transition-colors hover:bg-hover"
              >
                <span className="min-w-0">
                  <span className="block truncate text-[13px] font-semibold text-foreground">{item.title}</span>
                  <span className="block truncate text-[11.5px] text-muted-foreground">
                    {item.kind === 'task' ? `Task · ${item.subtitle}` : 'Follow-up'}
                  </span>
                </span>
                {item.day < today ? (
                  <span className="shrink-0 rounded-full bg-destructive/15 px-2 py-0.5 text-[11px] font-bold text-destructive">
                    Overdue
                  </span>
                ) : (
                  item.status && <DonorStatusBadge status={item.status} />
                )}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
