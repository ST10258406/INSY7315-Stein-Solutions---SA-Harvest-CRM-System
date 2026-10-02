import { useMemo, useState } from 'react';
import { Plus } from 'lucide-react';
import { ListPagination } from '@/components/common/ListPagination';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/store/authStore';
import { useMyTasks } from '../hooks';
import { TaskList } from '../components/TaskList';
import { TaskFormDialog } from '../components/TaskFormDialog';
import { taskMinDueDate } from '../schemas/taskForm.schema';
import type { MyTasksFilters, TaskDto } from '../types';

const PROCUREMENT_ROLES = ['Procurement', 'Admin', 'SuperAdmin'];
const DEFAULT_PAGE_SIZE = 20;
const SEGMENTS = [
  { value: 'open', label: 'Open' },
  { value: 'completed', label: 'Completed' },
] as const;

type Segment = (typeof SEGMENTS)[number]['value'];

export default function MyTasksPage() {
  const [segment, setSegment] = useState<Segment>('open');
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<TaskDto | null>(null);

  const roles = useAuthStore((s) => s.user?.roles) ?? [];
  const canManage = roles.some((role) => PROCUREMENT_ROLES.includes(role));

  const filters = useMemo<MyTasksFilters>(
    () => ({
      page,
      pageSize,
      isCompleted: segment === 'completed',
      ...(overdueOnly && segment === 'open' ? { dueBefore: taskMinDueDate() } : {}),
    }),
    [page, pageSize, segment, overdueOnly],
  );

  const { data, isPending, isError, error, refetch } = useMyTasks(filters);
  const tasks = data?.data ?? [];
  const pagination = data?.pagination;

  const changePageSize = (next: number) => {
    setPageSize(next);
    setPage(1);
  };

  const changeSegment = (next: Segment) => {
    setSegment(next);
    setPage(1);
  };

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_40px]">
      <div className="mb-5 flex flex-wrap items-end gap-6">
        <div>
          <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">My Tasks</h1>
          <p className="m-0 text-sm font-medium text-[var(--muted-c)]">Follow-ups and action items assigned to you.</p>
        </div>
        {canManage && (
          <div className="ml-auto">
            <Button size="sm" onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4 stroke-[2.2]" />
              <span>New Task</span>
            </Button>
          </div>
        )}
      </div>

      <section className="mb-4.5 flex flex-wrap items-center gap-3 rounded-2xl border border-[var(--border)] bg-[var(--soft)] p-4">
        <div className="flex items-center gap-1 rounded-full border border-[var(--border)] bg-[var(--card)] p-1">
          {SEGMENTS.map((seg) => {
            const active = segment === seg.value;
            return (
              <button
                key={seg.value}
                type="button"
                onClick={() => changeSegment(seg.value)}
                className={`h-8.5 rounded-full px-4 text-[13px] font-bold transition-colors ${
                  active ? 'bg-[var(--ink)] text-[var(--card)]' : 'text-[var(--muted-c)] hover:text-[var(--ink)]'
                }`}
              >
                {seg.label}
              </button>
            );
          })}
        </div>

        {segment === 'open' && (
          <button
            type="button"
            onClick={() => {
              setOverdueOnly((v) => !v);
              setPage(1);
            }}
            className={`flex h-10 items-center gap-2.5 rounded-full border px-4 text-[12.5px] font-semibold transition-colors ${
              overdueOnly
                ? 'border-[var(--brand-yellow)] bg-[var(--brand-yellow)] text-[#16160F]'
                : 'border-[var(--border)] bg-[var(--card)] text-[var(--ink)] hover:border-[var(--ink)]'
            }`}
          >
            <span
              className={`flex h-4.25 w-4.25 items-center justify-center rounded-[5px] border ${
                overdueOnly ? 'border-[#16160F] bg-[#16160F]' : 'border-[var(--border)] bg-[var(--field)]'
              }`}
            >
              {overdueOnly && (
                <svg viewBox="0 0 24 24" className="h-2.5 w-2.5" fill="none" stroke="var(--brand-yellow)" strokeWidth={3.4} strokeLinecap="round" strokeLinejoin="round">
                  <path d="m5.5 12.3 4 4 9-9.4" />
                </svg>
              )}
            </span>
            <span>Overdue only</span>
          </button>
        )}
      </section>

      <TaskList
        tasks={tasks}
        isPending={isPending}
        isError={isError}
        error={error}
        onRetry={refetch}
        onEditTask={setEditing}
        canEdit={canManage}
        canReopen={canManage}
        emptyTitle={segment === 'completed' ? 'No completed tasks' : 'No open tasks — nice work!'}
        emptyDescription={
          segment === 'completed'
            ? 'Tasks you complete will show up here.'
            : 'New follow-ups assigned to you will appear here automatically.'
        }
      />

      {pagination && pagination.totalCount > 0 && (
        <ListPagination
          pagination={pagination}
          onPageChange={setPage}
          onPageSizeChange={changePageSize}
          itemLabel="tasks"
        />
      )}

      {canManage && <TaskFormDialog mode="create" open={createOpen} onOpenChange={setCreateOpen} />}
      {editing && (
        <TaskFormDialog mode="edit" task={editing} open={!!editing} onOpenChange={(next) => !next && setEditing(null)} />
      )}
    </main>
  );
}
