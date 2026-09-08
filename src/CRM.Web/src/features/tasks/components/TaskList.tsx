import { ListChecks, TriangleAlert } from 'lucide-react';
import type { ApiError, TaskDto } from '../types';
import { TaskListItem } from './TaskListItem';

interface TaskListProps {
  tasks: TaskDto[];
  isPending: boolean;
  isError: boolean;
  error?: ApiError | null;
  onRetry: () => void;
  onEditTask: (task: TaskDto) => void;
  /** Hide the donor chip on each row (donor-scoped list). */
  showDonor?: boolean;
  canEdit?: boolean;
  canReopen?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
}

export function TaskList({
  tasks,
  isPending,
  isError,
  error,
  onRetry,
  onEditTask,
  showDonor = true,
  canEdit = false,
  canReopen = false,
  emptyTitle = 'No tasks',
  emptyDescription = 'Nothing to do here right now.',
}: TaskListProps) {
  if (isPending) {
    return (
      <div className="flex flex-col gap-2.5" aria-hidden>
        {Array.from({ length: 4 }).map((_, index) => (
          <div key={index} className="h-[92px] animate-pulse rounded-2xl bg-muted" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-2xl border border-[var(--border)] bg-[var(--card)] py-12 text-center">
        <TriangleAlert className="h-5 w-5 text-muted-foreground" />
        <p className="m-0 text-sm font-semibold text-foreground">Couldn't load tasks</p>
        <p className="m-0 text-xs text-muted-foreground">{error?.response?.data?.message ?? 'Something went wrong.'}</p>
        <button
          type="button"
          onClick={onRetry}
          className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
        >
          Retry
        </button>
      </div>
    );
  }

  if (tasks.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed border-border bg-card/60 py-14 text-center">
        <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-icon-bg">
          <ListChecks className="h-5 w-5 text-icon" />
        </span>
        <p className="m-0 text-sm font-bold tracking-tight text-foreground">{emptyTitle}</p>
        <p className="m-0 max-w-[300px] text-xs font-medium text-muted-foreground">{emptyDescription}</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-2.5">
      {tasks.map((task) => (
        <TaskListItem
          key={task.id}
          task={task}
          onEdit={onEditTask}
          showDonor={showDonor}
          canEdit={canEdit}
          canReopen={canReopen}
        />
      ))}
    </div>
  );
}
