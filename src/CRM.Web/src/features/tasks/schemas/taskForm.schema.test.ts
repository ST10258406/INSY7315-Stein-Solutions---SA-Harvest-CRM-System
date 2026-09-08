import { describe, it, expect } from 'vitest';
import {
  taskFormSchema,
  taskFormValuesToCreateRequest,
  taskFormValuesToUpdateRequest,
  type TaskFormValues,
} from './taskForm.schema';

const ymd = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
const daysFromNow = (days: number) => {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d;
};

const base: TaskFormValues = {
  donorId: 'donor-1',
  title: 'Confirm Thursday collection slot',
  description: 'Dineo is waiting on confirmation.',
  assignedToUserId: 'user-1',
  dueDate: ymd(daysFromNow(3)),
};

describe('taskFormSchema', () => {
  it('accepts a valid task', () => {
    expect(taskFormSchema.safeParse(base).success).toBe(true);
  });

  it('accepts a due date of today', () => {
    expect(taskFormSchema.safeParse({ ...base, dueDate: ymd(new Date()) }).success).toBe(true);
  });

  it('rejects a due date in the past', () => {
    expect(taskFormSchema.safeParse({ ...base, dueDate: ymd(daysFromNow(-1)) }).success).toBe(false);
  });

  it('rejects a missing title', () => {
    expect(taskFormSchema.safeParse({ ...base, title: '   ' }).success).toBe(false);
  });

  it('rejects a missing donor', () => {
    expect(taskFormSchema.safeParse({ ...base, donorId: '' }).success).toBe(false);
  });

  it('rejects a title longer than 255 characters', () => {
    expect(taskFormSchema.safeParse({ ...base, title: 'x'.repeat(256) }).success).toBe(false);
  });
});

describe('taskFormValuesToCreateRequest', () => {
  it('drops a blank description and omits donorId (it travels in the URL)', () => {
    const request = taskFormValuesToCreateRequest({ ...base, description: '   ' });
    expect(request).toEqual({
      title: base.title,
      assignedToUserId: 'user-1',
      dueDate: base.dueDate,
    });
  });
});

describe('taskFormValuesToUpdateRequest', () => {
  const original = {
    title: 'Old title',
    description: 'Old description',
    dueDate: '2026-09-10T00:00:00',
    assignedToUserId: 'user-1',
  };

  it('sends only the fields that changed', () => {
    const patch = taskFormValuesToUpdateRequest(
      { donorId: 'donor-1', title: 'New title', description: 'Old description', assignedToUserId: 'user-1', dueDate: '2026-09-10' },
      original,
    );
    expect(patch).toEqual({ title: 'New title' });
  });

  it('returns an empty patch when nothing changed', () => {
    const patch = taskFormValuesToUpdateRequest(
      { donorId: 'donor-1', title: 'Old title', description: 'Old description', assignedToUserId: 'user-1', dueDate: '2026-09-10' },
      original,
    );
    expect(patch).toEqual({});
  });
});
