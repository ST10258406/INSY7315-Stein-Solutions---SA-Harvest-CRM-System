import type { AxiosError } from 'axios';
import type { ApiErrorEnvelope } from '@/lib/apiError';

/** Error shape thrown by every users hook — matches the backend's standard envelope. */
export type ApiError = AxiosError<ApiErrorEnvelope>;

export type UserSortField = 'name' | 'email' | 'createdAt';

// Mirrors GetUsersQuery (CRM.Application.Modules.Users.Queries.GetUsers) exactly —
// keep these two in sync.
export interface UserFilters {
  page?: number;
  pageSize?: number;
  sortBy?: UserSortField;
  sortDir?: 'asc' | 'desc';
  search?: string;
  roleId?: string;
  isActive?: boolean;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PaginatedResult<T> {
  data: T[];
  pagination: PaginationMeta;
}

// Mirrors RoleDto (CRM.Application.Modules.Users.Dtos) — the four seeded roles
// (SuperAdmin, Admin, Procurement, Marketing), never hardcoded client-side.
export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
}

// Mirrors UserListItemDto.
export interface UserListItemDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roleId: string;
  role: string;
  isActive: boolean;
  /** True while a login lockout is in force — decided server-side. */
  isLockedOut: boolean;
  /** ISO timestamp the current lockout ends; null when not locked out. */
  lockedUntil: string | null;
  createdAt: string;
}

// Mirrors CreateUserRequest.
export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  roleId: string;
}

// Mirrors CreateUserResponseDto — temporaryPassword is present only in this
// one response, immediately after creation; it is never fetchable again.
export interface CreateUserResponseDto extends UserListItemDto {
  temporaryPassword: string;
}

// Mirrors UpdateUserRequest.
export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
}

// Mirrors ChangeUserRoleRequest.
export interface ChangeUserRoleRequest {
  roleId: string;
}
