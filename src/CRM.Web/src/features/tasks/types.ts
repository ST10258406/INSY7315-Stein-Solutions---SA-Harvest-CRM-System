import type { ApiError, PaginatedResult } from '@/features/donors/types';

export type { ApiError, PaginatedResult };

export interface TaskUserDto {
  id: string;
  fullName: string;
}

export interface TaskDonorDto {
  id: string;
  companyName: string;
}

/**
 * The single shared task shape returned by both GET /tasks and
 * GET /donors/{id}/tasks. Mirrors CRM.Application.Modules.Tasks.Dtos.TaskDto.
 * `dueDate` / `completedAt` / `createdAt` are ISO strings (dueDate is date-only
 * on the wire, rendered midnight).
 */
export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  dueDate: string;
  isCompleted: boolean;
  completedAt: string | null;
  createdAt: string;
  donor: TaskDonorDto;
  assignedTo: TaskUserDto;
  createdBy: TaskUserDto;
  completedBy: TaskUserDto | null;
}

/** Mirrors GetMyTasksQuery. `assignedTo` is taken from the JWT, never sent. */
export interface MyTasksFilters {
  page?: number;
  pageSize?: number;
  /** Defaults to false server-side (open tasks first). */
  isCompleted?: boolean;
  /** yyyy-MM-dd — tasks due strictly before this date. */
  dueBefore?: string;
  donorId?: string;
}

/** Mirrors GetDonorTasksQuery — tri-state completion filter. */
export interface DonorTasksFilters {
  page?: number;
  pageSize?: number;
  isCompleted?: 'true' | 'false' | 'all';
}

/** Request body for POST /api/v1/donors/{id}/tasks — mirrors CreateTaskRequest. */
export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  assignedToUserId: string;
  /** yyyy-MM-dd; must be today or later. */
  dueDate: string;
}

/** Request body for PATCH /api/v1/tasks/{id} — every field optional, null = unchanged. */
export interface UpdateTaskRequest {
  title?: string;
  description?: string | null;
  dueDate?: string;
  assignedToUserId?: string;
}
