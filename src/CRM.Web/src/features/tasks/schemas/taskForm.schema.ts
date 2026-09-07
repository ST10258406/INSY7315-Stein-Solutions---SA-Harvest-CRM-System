import { z } from 'zod';
import type { CreateTaskRequest, UpdateTaskRequest } from '../types';

// Mirrors CreateTaskCommandValidator / UpdateTaskCommandValidator (CRM.Application):
//  - title:            required, max 255
//  - description:       optional, max 10 000
//  - assignedToUserId:  required (a real user id)
//  - dueDate:           today or later
const MAX_TITLE = 255;
const MAX_DESCRIPTION = 10_000;

function todayYmd(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function isTodayOrLater(value: string): boolean {
  if (!value) return false;
  const picked = new Date(`${value}T00:00:00`);
  if (Number.isNaN(picked.getTime())) return false;
  const startOfToday = new Date();
  startOfToday.setHours(0, 0, 0, 0);
  return picked.getTime() >= startOfToday.getTime();
}

export { todayYmd as taskMinDueDate };

export const taskFormSchema = z.object({
  donorId: z.string().trim().min(1, 'Select a donor'),
  title: z.string().trim().min(1, 'Give the task a title').max(MAX_TITLE, `Title must be ${MAX_TITLE} characters or fewer`),
  description: z
    .string()
    .trim()
    .max(MAX_DESCRIPTION, `Keep it under ${MAX_DESCRIPTION.toLocaleString()} characters`)
    .optional(),
  assignedToUserId: z.string().trim().min(1, 'Pick an assignee'),
  dueDate: z
    .string()
    .trim()
    .min(1, 'Pick a due date')
    .refine(isTodayOrLater, { message: 'Due date must be today or later' }),
});

export type TaskFormValues = z.infer<typeof taskFormSchema>;

export function emptyTaskFormValues(donorId = '', assignedToUserId = ''): TaskFormValues {
  return { donorId, title: '', description: '', assignedToUserId, dueDate: '' };
}

/** Form state → POST body (donorId travels in the URL, not the body). */
export function taskFormValuesToCreateRequest(values: TaskFormValues): CreateTaskRequest {
  const description = values.description?.trim();
  return {
    title: values.title.trim(),
    assignedToUserId: values.assignedToUserId,
    dueDate: values.dueDate,
    ...(description ? { description } : {}),
  };
}

/** Form state → PATCH body, sending only fields that changed from `original`. */
export function taskFormValuesToUpdateRequest(
  values: TaskFormValues,
  original: { title: string; description: string | null; dueDate: string; assignedToUserId: string },
): UpdateTaskRequest {
  const patch: UpdateTaskRequest = {};
  const title = values.title.trim();
  const description = values.description?.trim() ?? '';
  const dueDate = values.dueDate;

  if (title !== original.title) patch.title = title;
  if (description && description !== (original.description ?? '')) patch.description = description;
  if (dueDate && dueDate !== original.dueDate.slice(0, 10)) patch.dueDate = dueDate;
  if (values.assignedToUserId && values.assignedToUserId !== original.assignedToUserId) {
    patch.assignedToUserId = values.assignedToUserId;
  }
  return patch;
}
