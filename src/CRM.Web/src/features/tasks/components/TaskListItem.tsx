import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Check, Calendar, TriangleAlert, MoreHorizontal, Pencil, RotateCcw } from 'lucide-react';
import { paths } from '@/routes/paths';
import { getInitials, getAvatarColor } from '@/features/donors/lib/avatar';
import { formatDate, isOverdue } from '@/features/donors/lib/donorFormatters';
import { useCompleteTask, useReopenTask } from '../hooks';
import type { TaskDto } from '../types';

interface TaskListItemProps {
  task: TaskDto;
  onEdit: (task: TaskDto) => void;
  /** Hide the donor chip when the list is already scoped to one donor. */
  showDonor?: boolean;
  /** Reopen is Procurement+ only; the API still enforces it. */
  canReopen?: boolean;
  /** Edit is Procurement+ only; the API still enforces it. */
  canEdit?: boolean;
}

export function TaskListItem({ task, onEdit, showDonor = true, canReopen = false, canEdit = false }: TaskListItemProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const complete = useCompleteTask();
  const reopen = useReopenTask();

  const late = !task.isCompleted && isOverdue(task.dueDate);
  const busy = complete.isPending || reopen.isPending;
  const assignee = task.assignedTo?.fullName ?? '—';

  const toggleDone = () => {
    if (busy) return;
    if (task.isCompleted) {
      if (canReopen) reopen.mutate(task.id);
    } else {
      complete.mutate(task.id);
    }
    setMenuOpen(false);
  };

  return (
    <article
      className={`flex items-start gap-3.5 rounded-2xl border border-[var(--border)] bg-[var(--card)] p-[18px_18px_18px_20px] shadow-[0_1px_3px_var(--shadow)] ${
        task.isCompleted ? 'opacity-70' : ''
      } ${late ? 'border-l-[3px] border-l-[var(--brand-red)]' : ''}`}
    >
      <button
        type="button"
        onClick={toggleDone}
        disabled={busy || (task.isCompleted && !canReopen)}
        aria-pressed={task.isCompleted}
        aria-label={task.isCompleted ? 'Reopen task' : 'Mark task complete'}
        title={task.isCompleted ? (canReopen ? 'Reopen task' : 'Reopen (Procurement only)') : 'Mark complete'}
        className={`mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-[1.8px] transition-colors disabled:cursor-not-allowed ${
          task.isCompleted
            ? 'border-[var(--ink)] bg-[var(--ink)] text-[var(--brand-yellow)]'
            : 'border-[var(--border)] bg-[var(--field)] hover:border-[var(--ink)]'
        }`}
      >
        {task.isCompleted && <Check className="h-3.25 w-3.25 stroke-[3.4]" />}
      </button>

      <div className="min-w-0 flex-1">
        <div
          className={`text-sm font-extrabold leading-snug tracking-tight [text-wrap:pretty] ${
            task.isCompleted ? 'text-[var(--muted-c)] line-through' : 'text-[var(--ink)]'
          }`}
        >
          {task.title}
        </div>

        {task.description && (
          <p className="m-0 mt-1.5 truncate text-[12.5px] font-medium leading-relaxed text-[var(--muted-c)]">
            {task.description}
          </p>
        )}

        <div className="mt-3 flex flex-wrap items-center gap-2.5">
          {showDonor && (
            <Link
              to={paths.donorDetail(task.donor.id)}
              className="inline-flex items-center gap-1.75 rounded-[10px] bg-[var(--chip)] px-2.75 py-1.25 text-[11.5px] font-bold text-[var(--ink)] hover:text-[var(--brand)]"
            >
              {task.donor.companyName}
            </Link>
          )}

          <span
            className={`inline-flex items-center gap-1.5 rounded-[10px] px-2.5 py-1.25 text-[11.5px] font-bold ${
              late ? 'bg-[#FBE9E9] text-[#9B2C2C]' : 'bg-[var(--chip)] text-[var(--muted-c)]'
            }`}
          >
            {late ? <TriangleAlert className="h-3 w-3" /> : <Calendar className="h-3 w-3" />}
            <span>
              {task.isCompleted ? `Completed ${formatDate(task.completedAt)}` : `Due ${formatDate(task.dueDate)}`}
              {late ? ' · overdue' : ''}
            </span>
          </span>

          <span className="inline-flex items-center gap-2">
            <span
              className="flex h-5.5 w-5.5 items-center justify-center rounded-full text-[9px] font-bold text-white"
              style={{ backgroundColor: getAvatarColor(assignee) }}
            >
              {getInitials(assignee)}
            </span>
            <span className="text-[11.5px] font-semibold text-[var(--muted2)]">{assignee}</span>
          </span>
        </div>
      </div>

      {(canEdit || (task.isCompleted && canReopen)) && (
        <div className="relative shrink-0">
          <button
            type="button"
            onClick={() => setMenuOpen((v) => !v)}
            aria-label="Task actions"
            className="flex h-8 w-8 items-center justify-center rounded-full border border-transparent text-[var(--icon)] transition-colors hover:border-[var(--border)] hover:bg-[var(--hover)]"
          >
            <MoreHorizontal className="h-4 w-4" />
          </button>
          {menuOpen && (
            <>
              <div className="fixed inset-0 z-10" onClick={() => setMenuOpen(false)} aria-hidden />
              <div className="absolute right-0 top-9 z-20 flex w-44 flex-col gap-0.5 rounded-xl border border-[var(--border)] bg-[var(--card)] p-1.5 text-left shadow-[0_12px_28px_rgba(20,20,15,0.16)]">
                {canEdit && (
                  <button
                    type="button"
                    onClick={() => {
                      setMenuOpen(false);
                      onEdit(task);
                    }}
                    className="flex h-9 items-center gap-2.5 rounded-lg px-3 text-[13px] font-semibold text-[var(--ink)] transition-colors hover:bg-[var(--hover)]"
                  >
                    <Pencil className="h-3.75 w-3.75 text-[var(--icon)]" />
                    <span>Edit</span>
                  </button>
                )}
                {task.isCompleted && canReopen && (
                  <button
                    type="button"
                    onClick={toggleDone}
                    className="flex h-9 items-center gap-2.5 rounded-lg px-3 text-[13px] font-semibold text-[var(--ink)] transition-colors hover:bg-[var(--hover)]"
                  >
                    <RotateCcw className="h-3.75 w-3.75 text-[var(--icon)]" />
                    <span>Reopen</span>
                  </button>
                )}
              </div>
            </>
          )}
        </div>
      )}
    </article>
  );
}
