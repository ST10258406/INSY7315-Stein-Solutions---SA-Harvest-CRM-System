import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, Lock } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { useAuthStore } from '@/store/authStore';
import { getInitials, getAvatarColor } from '@/features/donors/lib/avatar';
import { applyServerErrors } from '@/features/donors/lib/applyServerErrors';
import { useCreateTask, useUpdateTask } from '../hooks';
import { DonorSelectField } from './DonorSelectField';
import {
  taskFormSchema,
  emptyTaskFormValues,
  taskFormValuesToCreateRequest,
  taskFormValuesToUpdateRequest,
  taskMinDueDate,
  type TaskFormValues,
} from '../schemas/taskForm.schema';
import type { TaskDto } from '../types';

type TaskFormDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
} & (
  | { mode: 'create'; task?: undefined; fixedDonorId?: string; fixedDonorName?: string }
  | { mode: 'edit'; task: TaskDto; fixedDonorId?: undefined; fixedDonorName?: undefined }
);

export function TaskFormDialog(props: TaskFormDialogProps) {
  const { open, onOpenChange, mode } = props;
  const currentUser = useAuthStore((s) => s.user);
  const [generalError, setGeneralError] = useState<string | null>(null);

  // Assignee can't be changed yet — there is no staff-directory endpoint (same
  // blocker as the donor form's relationship-manager picker, Issue #45). New
  // tasks are assigned to the creator; editing keeps the existing assignee.
  const assignee =
    mode === 'edit'
      ? { id: props.task.assignedTo.id, name: props.task.assignedTo.fullName }
      : { id: currentUser?.id ?? '', name: currentUser ? `${currentUser.firstName} ${currentUser.lastName}` : '—' };

  const defaults: TaskFormValues =
    mode === 'edit'
      ? {
          donorId: props.task.donor.id,
          title: props.task.title,
          description: props.task.description ?? '',
          assignedToUserId: props.task.assignedTo.id,
          dueDate: props.task.dueDate.slice(0, 10),
        }
      : emptyTaskFormValues(props.fixedDonorId ?? '', assignee.id);

  const createTask = useCreateTask();
  const updateTask = useUpdateTask(mode === 'edit' ? props.task.id : '');

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskFormSchema),
    defaultValues: defaults,
  });

  const donorId = useWatch({ control, name: 'donorId' });
  const donorFixed = mode === 'create' && !!props.fixedDonorId;

  const handleOpenChange = (next: boolean) => {
    if (!next) {
      reset(defaults);
      setGeneralError(null);
      createTask.reset();
      updateTask.reset();
    }
    onOpenChange(next);
  };

  const onSubmit = handleSubmit(async (values) => {
    setGeneralError(null);
    try {
      if (mode === 'create') {
        await createTask.mutateAsync({
          donorId: values.donorId,
          request: taskFormValuesToCreateRequest(values),
        });
      } else {
        const patch = taskFormValuesToUpdateRequest(values, {
          title: props.task.title,
          description: props.task.description,
          dueDate: props.task.dueDate,
          assignedToUserId: props.task.assignedTo.id,
        });
        if (Object.keys(patch).length > 0) await updateTask.mutateAsync(patch);
      }
      handleOpenChange(false);
    } catch (error) {
      const mapped = applyServerErrors(error as Parameters<typeof applyServerErrors>[0], setError);
      if (!mapped) setGeneralError('Could not save this task. Please try again.');
    }
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'New task' : 'Edit task'}</DialogTitle>
          <DialogDescription>Assign a follow-up against a donor record.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="flex flex-col gap-3.5">
          {generalError && (
            <div className="flex items-center gap-2 rounded-2xl border border-destructive/40 bg-destructive/10 px-3.5 py-2.5 text-xs font-semibold text-destructive">
              <AlertCircle className="h-3.75 w-3.75 shrink-0" />
              <span>{generalError}</span>
            </div>
          )}

          {donorFixed ? (
            <div className="flex flex-col gap-2">
              <span className="text-xs font-bold text-foreground">Donor</span>
              <div className="flex h-11 items-center rounded-2xl border border-border bg-field px-3.75 text-[13.5px] font-semibold text-foreground">
                {props.fixedDonorName ?? 'This donor'}
              </div>
            </div>
          ) : (
            <DonorSelectField
              value={donorId}
              onChange={(id) => setValue('donorId', id, { shouldValidate: true })}
              error={errors.donorId?.message}
              initialLabel={mode === 'edit' ? props.task.donor.companyName : undefined}
            />
          )}

          <div className="flex flex-col gap-2">
            <label htmlFor="task-title" className="text-xs font-bold text-foreground">
              Title <span aria-hidden className="text-destructive">*</span>
            </label>
            <input
              id="task-title"
              {...register('title')}
              placeholder="e.g. Confirm Thursday collection slot"
              className={`h-11 rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.title ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.title && <span className="text-xs text-destructive">{errors.title.message}</span>}
          </div>

          <div className="flex flex-col gap-2">
            <label htmlFor="task-description" className="text-xs font-bold text-foreground">
              Description
            </label>
            <textarea
              id="task-description"
              rows={3}
              {...register('description')}
              placeholder="Optional detail for whoever picks this up."
              className={`resize-y rounded-2xl border bg-field px-3.75 py-3.5 text-[13.5px] font-medium leading-relaxed text-foreground outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-brand/60 ${
                errors.description ? 'border-destructive/70' : 'border-border'
              }`}
            />
            {errors.description && <span className="text-xs text-destructive">{errors.description.message}</span>}
          </div>

          <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <span className="text-xs font-bold text-foreground">Assigned to</span>
              <div className="flex h-11 items-center gap-2 rounded-2xl border border-border bg-field px-3 text-[13px] font-semibold text-foreground">
                <span
                  className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-[9px] font-bold text-white"
                  style={{ backgroundColor: getAvatarColor(assignee.name) }}
                >
                  {getInitials(assignee.name)}
                </span>
                <span className="min-w-0 flex-1 truncate">{assignee.name}</span>
                <Lock className="h-3.25 w-3.25 shrink-0 text-muted-foreground" />
              </div>
              <span className="text-[11.5px] font-medium text-muted-foreground">
                {mode === 'create' ? 'New tasks are assigned to you.' : 'Reassignment needs the staff directory.'}
              </span>
            </div>

            <div className="flex flex-col gap-2">
              <label htmlFor="task-due" className="text-xs font-bold text-foreground">
                Due date <span aria-hidden className="text-destructive">*</span>
              </label>
              <input
                id="task-due"
                type="date"
                min={taskMinDueDate()}
                {...register('dueDate')}
                className={`h-11 w-full rounded-2xl border bg-field px-3.75 text-[13.5px] font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-brand/60 ${
                  errors.dueDate ? 'border-destructive/70' : 'border-border'
                }`}
              />
              {errors.dueDate ? (
                <span className="text-xs text-destructive">{errors.dueDate.message}</span>
              ) : (
                <span className="text-[11.5px] font-medium text-muted-foreground">Must be today or later.</span>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" size="sm" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : mode === 'create' ? 'Create task' : 'Save changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
