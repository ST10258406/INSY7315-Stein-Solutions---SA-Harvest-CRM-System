export type { ApiError } from '@/features/donors/types';

/** Mirrors SavedFilterDto (CRM.Application). */
export interface SavedFilterDto {
  id: string;
  name: string;
  page: 'Donors';
  /** Donor-list query string without the leading '?', e.g. "status=Active&regionCode=GP". */
  queryString: string;
}

export interface CreateSavedFilterRequest {
  name: string;
  page: 'Donors';
  queryString: string;
}
