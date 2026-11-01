import { useState, type ComponentProps, type MouseEvent, type Ref } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { labelDayButton } from 'react-day-picker';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Calendar, CalendarDayButton } from '@/components/ui/calendar';
import { Button } from '@/components/ui/button';
import { fromYmd, toYmd } from '@/lib/date';
import { paths } from '@/routes/paths';
import { useAuthStore } from '@/store/authStore';
import { PROCUREMENT_OR_ABOVE } from '@/features/shell/navItems';
import { useMyTasks } from '@/features/tasks/hooks';
import { TaskFormDialog } from '@/features/tasks/components/TaskFormDialog';
import { donorKeys, fetchDonors } from '@/features/donors/hooks';
import { DonorStatusBadge } from '@/features/donors/components/DonorStatusBadge';
import { addDaysYmd, monthBounds, sastYmd } from '../lib/sast';

// The API's page-size cap. A month with more is flagged rather than silently cut.
const MAX_ITEMS = 100;
// Dot colour per kind, shared by the day cells, the legend and the agenda rows.
const KIND_DOT = { task: 'bg-sky-500', followUp: 'bg-emerald-500' } as const;

interface CalendarDialogProps {
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
export function CalendarDialog({ open, onOpenChange }: CalendarDialogProps) {
  // Double-clicking a day swaps the calendar for a new task due that day, so the two never stack.
  const [newTaskDay, setNewTaskDay] = useState<string | null>(null);

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="w-[860px] max-w-[calc(100vw-2rem)]">
          <DialogHeader>
            <DialogTitle>Calendar</DialogTitle>
            <DialogDescription>Your open tasks and your donors' follow-ups. Pick a day to see what's on.</DialogDescription>
          </DialogHeader>
          {/* Mounted only while open, so closing the dialog doesn't keep fetching. */}
          {open && (
            <CalendarBody
              onNavigate={() => onOpenChange(false)}
              onNewTask={(day) => {
                onOpenChange(false);
                setNewTaskDay(day);
              }}
            />
          )}
        </DialogContent>
      </Dialog>
      {/* Mounted per day so the form's default due date is fresh each time. */}
      {newTaskDay && (
        <TaskFormDialog
          key={newTaskDay}
          mode="create"
          initialDueDate={newTaskDay}
          open
          onOpenChange={(next) => !next && setNewTaskDay(null)}
        />
      )}
    </>
  );
}

// The shadcn Calendar defines Root inline, so every render remounts the grid and a double-click's two
// clicks land on different nodes (no dblclick). A module-level Root keeps the grid mounted.
function StableRoot({ rootRef, ...props }: ComponentProps<'div'> & { rootRef?: Ref<HTMLDivElement> }) {
  return <div data-slot="calendar" ref={rootRef} {...props} />;
}

// Module-level so DayPicker keeps the same component between renders instead of remounting every day cell.
function ItemsDayButton({ children, ...props }: ComponentProps<typeof CalendarDayButton>) {
  const { hasTask, hasFollowUp } = props.modifiers;
  return (
    <CalendarDayButton {...props} className="text-[13.5px]">
      {children}
      {/* Absolute so the dots don't push the day number off-centre in the selection circle. */}
      <span aria-hidden className="absolute bottom-1 left-1/2 flex -translate-x-1/2 gap-1">
        {hasTask && <span className={`h-1.5 w-1.5 rounded-full ${KIND_DOT.task}`} />}
        {hasFollowUp && <span className={`h-1.5 w-1.5 rounded-full ${KIND_DOT.followUp}`} />}
      </span>
    </CalendarDayButton>
  );
}

function CalendarBody({ onNavigate, onNewTask }: { onNavigate: () => void; onNewTask: (day: string) => void }) {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const canSeeDonors = (user?.roles ?? []).some((role) => PROCUREMENT_OR_ABOVE.includes(role));

  const today = sastYmd(new Date());
  const thisMonth = monthBounds(today).first;
  const [month, setMonth] = useState(thisMonth);
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

  // A double-click's first click has already selected the day. Past days are skipped: the API rejects past due dates.
  const onDayDoubleClick = (e: MouseEvent) => {
    if (canSeeDonors && selected >= today && (e.target as HTMLElement).closest('button[data-day]')) onNewTask(selected);
  };

  const select = (day: string) => {
    setSelected(day);
    setMonth(monthBounds(day).first); // clicking a greyed outside day jumps to its month
  };

  return (
    <div className="grid min-h-0 flex-1 gap-6 overflow-y-auto md:grid-cols-[auto_1fr]">
      <div onDoubleClick={onDayDoubleClick}>
        <Calendar
          mode="single"
          required
          month={fromYmd(month)}
          onMonthChange={(date) => setMonth(monthBounds(toYmd(date)).first)}
          selected={fromYmd(selected)}
          onSelect={(date) => date && select(toYmd(date))}
          modifiers={{
            overdue: (date) => isOverdueDay(toYmd(date)),
            hasTask: (date) => !!byDay.get(toYmd(date))?.some((i) => i.kind === 'task'),
            hasFollowUp: (date) => !!byDay.get(toYmd(date))?.some((i) => i.kind === 'followUp'),
          }}
          modifiersClassNames={{ overdue: 'text-destructive font-bold' }}
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
          components={{ Root: StableRoot, DayButton: ItemsDayButton }}
          className="mx-auto rounded-2xl border border-border p-3 [--cell-size:--spacing(11)]"
        />
        <div className="mt-3 flex items-center justify-between gap-3">
          <ul aria-label="Legend" className="m-0 flex list-none gap-3 p-0 text-[11.5px] text-muted-foreground">
            <li className="flex items-center gap-1.5">
              <span className={`h-2 w-2 rounded-full ${KIND_DOT.task}`} />
              Task
            </li>
            {canSeeDonors && (
              <li className="flex items-center gap-1.5">
                <span className={`h-2 w-2 rounded-full ${KIND_DOT.followUp}`} />
                Follow-up
              </li>
            )}
            <li className="flex items-center gap-1.5">
              <span className="font-bold text-destructive">12</span>
              Overdue
            </li>
          </ul>
          <Button variant="outline" size="sm" onClick={() => select(today)} disabled={selected === today && month === thisMonth}>
            Today
          </Button>
        </div>
        <div aria-live="polite" className="mt-2 text-[12.5px] text-muted-foreground">
          {isLoading && 'Loading…'}
          {isError && <span className="text-destructive">Couldn't load the calendar. Try again later.</span>}
          {!isLoading && !isError && items.length === 0 && 'Nothing due this month.'}
          {!isLoading && !isError && items.length > 0 && `${items.length} ${items.length === 1 ? 'item' : 'items'} this month.`}
          {truncated && <span className="block">Showing the first {MAX_ITEMS} items.</span>}
        </div>
        {canSeeDonors && <p className="m-0 mt-1 text-[11.5px] text-muted-foreground">Double-click a day to add a task.</p>}
      </div>

      <section className="flex min-h-0 flex-col md:max-h-[460px] md:border-l md:border-border md:pl-6">
        <h3 className="m-0 mb-3 text-[14px] font-extrabold text-foreground">
          {selected === today && 'Today · '}
          {fromYmd(selected).toLocaleDateString('en-ZA', { weekday: 'long', day: 'numeric', month: 'long' })}
        </h3>
        {dayItems.length === 0 ? (
          <p className="m-0 rounded-2xl border border-dashed border-border px-4 py-8 text-center text-[12.5px] text-muted-foreground">
            Nothing on this day.
          </p>
        ) : (
          <ul className="m-0 flex min-h-0 list-none flex-col gap-1.5 overflow-y-auto p-0">
            {dayItems.map((item) => (
              <li key={item.key}>
                <button
                  type="button"
                  onClick={() => open(item)}
                  className="flex w-full items-center justify-between gap-3 rounded-2xl border border-border bg-card px-3.5 py-2.5 text-left transition-colors hover:bg-hover"
                >
                  <span className="flex min-w-0 items-center gap-3">
                    <span aria-hidden className={`h-2 w-2 shrink-0 rounded-full ${KIND_DOT[item.kind]}`} />
                    <span className="min-w-0">
                      <span className="block truncate text-[13px] font-semibold text-foreground">{item.title}</span>
                      <span className="block truncate text-[11.5px] text-muted-foreground">
                        {item.kind === 'task' ? `Task · ${item.subtitle}` : 'Follow-up'}
                      </span>
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
      </section>
    </div>
  );
}
