import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import type { ApiError } from '../types';

// FluentValidation property paths (see CreateUserCommandValidator /
// UpdateUserCommandValidator) come back as e.g. "Request.Email" — strip the
// leading "Request" segment and lowercase each remaining segment's first
// letter to land on this form's field paths ("email"). Mirrors the donors
// feature's applyServerErrors exactly.
function toFormFieldPath(backendField: string): string {
  return backendField
    .split('.')
    .filter((segment) => segment.toLowerCase() !== 'request')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.');
}

export function applyServerErrors<T extends FieldValues>(error: ApiError, setError: UseFormSetError<T>): boolean {
  const errors = error.response?.data?.errors;
  if (!errors || errors.length === 0) return false;

  let mapped = false;
  for (const { field, message } of errors) {
    const path = toFormFieldPath(field);
    if (!path) continue;
    setError(path as Path<T>, { type: 'server', message });
    mapped = true;
  }
  return mapped;
}
