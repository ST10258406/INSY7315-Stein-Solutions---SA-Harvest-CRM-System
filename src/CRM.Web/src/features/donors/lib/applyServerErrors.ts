import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import type { ApiError } from '../types';

// FluentValidation property paths (see CreateDonorCommandValidator /
// UpdateDonorCommandValidator) come back as e.g. "Request.Company.IncomeTaxNumber"
// or "Request.Donations.TypeIds" — strip the leading "Request" segment and
// lowercase each remaining segment's first letter to land on this form's
// field paths ("company.incomeTaxNumber", "donations.typeIds").
function toFormFieldPath(backendField: string): string {
  return backendField
    .split('.')
    .filter((segment) => segment.toLowerCase() !== 'request')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.');
}

/**
 * Maps a 400 VALIDATION_ERROR envelope's field-level errors onto the
 * matching react-hook-form fields, so they render under the right input
 * instead of only surfacing as a generic toast. Returns true when at least
 * one error was mapped, so the caller knows whether a fallback banner is
 * still needed.
 */
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
