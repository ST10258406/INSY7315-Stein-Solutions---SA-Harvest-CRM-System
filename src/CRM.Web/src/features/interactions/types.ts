import type { ApiError, PaginatedResult } from '@/features/donors/types';

export type { ApiError, PaginatedResult };

// Mirrors CRM.Domain.Enums.InteractionType exactly (the backend validator parses
// the raw string against this enum — see LogInteractionCommandValidator). Keep in sync.
export const INTERACTION_TYPES = [
  'Note',
  'Email',
  'Call',
  'Meeting',
  'FormSubmission',
  'FollowUp',
] as const;

export type InteractionType = (typeof INTERACTION_TYPES)[number];

/**
 * Types a staff member may pick when logging an interaction by hand.
 * `FormSubmission` and `FollowUp` are system-generated (public-form submissions
 * and the LogInteraction follow-up side effect) so they are shown in the feed
 * but never offered as a choice in the log form.
 */
export const LOGGABLE_INTERACTION_TYPES = ['Note', 'Call', 'Email', 'Meeting'] as const satisfies readonly InteractionType[];

/** One row of a donor's append-only interaction timeline. Mirrors InteractionLogDto. */
export interface InteractionLogDto {
  id: string;
  donorId: string;
  interactionType: string;
  subject: string | null;
  body: string;
  /** Short-lived (15-min) SAS URL, generated at query time — never a raw blob path. */
  emailAttachmentUrl: string | null;
  createdAt: string;
  createdBy: InteractionUserDto;
}

export interface InteractionUserDto {
  id: string;
  fullName: string;
}

/** Mirrors GetDonorInteractionsQuery. `donorId` is a path param, not sent as a filter. */
export interface InteractionFilters {
  page?: number;
  pageSize?: number;
  interactionType?: InteractionType;
}

/** Request body for POST /api/v1/donors/{id}/interactions — mirrors LogInteractionRequest. */
export interface LogInteractionRequest {
  interactionType: InteractionType;
  subject?: string | null;
  body: string;
  /** ISO 8601 UTC; must be strictly in the future when present. */
  followUpDate?: string | null;
}
