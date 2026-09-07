import { useState } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/store/authStore';
import { useDonorTasks } from '../hooks';
import { TaskList } from './TaskList';
import { TaskFormDialog } from './TaskFormDialog';
import type { DonorTasksFilters, TaskDto } from '../types';

const PROCUREMENT_ROLES = ['Procurement', 'Admin', 'SuperAdmin'];

interface DonorTasksPanelProps {
  donorId: string;
  donorName: string;
}

/** The Tasks column of the donor detail Activity tab (Claude design's two-column view). */
export function DonorTasksPanel({ donorId, donorName }: DonorTasksPanelProps) {
  const [showCompleted, setShowCompleted] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<TaskDto | null>(null);

  const roles = useAuthStore((s) => s.user?.roles) ?? [];
  const canManage = roles.some((role) => PROCUREMENT_ROLES.includes(role));

  const filters: DonorTasksFilters = { isCompleted: showCompleted ? 'all' : 'false', pageSize: 50 };
  const { data, isPending, isError, error, refetch } = useDonorTasks(donorId, filters);
  const tasks = data?.data ?? [];

  return (
    <section>
      <div className="mb-3.5 flex items-center justify-between gap-3.5">
        <h2 className="m-0 text-[15px] font-extrabold tracking-tight text-[var(--ink)]">Tasks</h2>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setShowCompleted((v) => !v)}
            className={`h-8.5 rounded-full border px-3 text-[11.5px] font-bold transition-colors ${
              showCompleted
                ? 'border-[var(--ink)] bg-[var(--ink)] text-[var(--card)]'
                : 'border-[var(--border)] bg-[var(--card)] text-[var(--muted-c)] hover:border-[var(--ink)]'
            }`}
          >
            {showCompleted ? 'All' : 'Open only'}
          </button>
          {canManage && (
            <Button variant="secondary" size="sm" onClick={() => setCreateOpen(true)}>
              <Plus className="h-3.5 w-3.5 stroke-[2.2]" />
              <span>Add Task</span>
            </Button>
          )}
        </div>
      </div>

      <TaskList
        tasks={tasks}
        isPending={isPending}
        isError={isError}
        error={error}
        onRetry={refetch}
        onEditTask={setEditing}
        showDonor={false}
        canEdit={canManage}
        canReopen={canManage}
        emptyTitle="No tasks for this donor"
        emptyDescription={canManage ? 'Add the first follow-up task.' : 'No follow-up tasks have been created yet.'}
      />

      {canManage && (
        <TaskFormDialog
          mode="create"
          open={createOpen}
          onOpenChange={setCreateOpen}
          fixedDonorId={donorId}
          fixedDonorName={donorName}
        />
      )}
      {editing && (
        <TaskFormDialog
          mode="edit"
          task={editing}
          open={!!editing}
          onOpenChange={(next) => !next && setEditing(null)}
        />
      )}
    </section>
  );
}
