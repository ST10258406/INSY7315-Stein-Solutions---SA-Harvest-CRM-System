import { z } from 'zod';

// Mirrors CreateSavedFilterCommandValidator: name required, at most 100 characters after trimming.
export const MAX_SAVED_FILTER_NAME = 100;

export const saveFilterSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Enter a name')
    .max(MAX_SAVED_FILTER_NAME, `Name must be ${MAX_SAVED_FILTER_NAME} characters or fewer`),
});

export type SaveFilterFormValues = z.infer<typeof saveFilterSchema>;

const PAGING_KEYS = ['page', 'pageSize'];

/** The donor list's current query string without paging, so a saved filter opens on page 1. */
export function filterQueryString(search: string): string {
  const params = new URLSearchParams(search);
  for (const key of PAGING_KEYS) params.delete(key);
  return params.toString();
}
