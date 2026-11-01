import type { VettingSubmissionFilters } from '../types';

/** Single source of truth for vetting query keys (same pattern as donorKeys). */
export const vettingKeys = {
  all: ['vetting'] as const,
  submissions: () => [...vettingKeys.all, 'submissions'] as const,
  submissionList: (filters: VettingSubmissionFilters = {}) => [...vettingKeys.submissions(), 'list', filters] as const,
  submission: (id: string | undefined) => [...vettingKeys.submissions(), 'detail', id] as const,
  /** Thumbnail SAS URL; keyed by capture time so a re-upload fetches the new blob. */
  photoUrl: (id: string, shotIndex: number, capturedAt: string | null) =>
    [...vettingKeys.submission(id), 'photo-url', shotIndex, capturedAt] as const,
  officers: () => [...vettingKeys.all, 'officers'] as const,
  officer: (id: string | undefined) => [...vettingKeys.officers(), id] as const,
  siteVisits: () => [...vettingKeys.all, 'site-visits'] as const,
  siteVisitDay: (date: string, officerId?: string) => [...vettingKeys.siteVisits(), date, officerId ?? null] as const,
};
