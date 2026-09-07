import type { ApiError, PaginationMeta } from '@/features/donors/types';

export type { ApiError };

// Mirrors CRM.Domain.Enums.NotificationType.
export const NOTIFICATION_TYPES = [
  'FollowUpReminder',
  'NewDonorPendingReview',
  'TaskDue',
  'TaskAssigned',
  'DonorApproved',
  'DonorRejected',
] as const;

export type NotificationType = (typeof NOTIFICATION_TYPES)[number];

/** One in-app notification. Mirrors NotificationDto. */
export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  readAt: string | null;
  notificationType: string;
  /** Loose polymorphic reference — no real FK. */
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  createdAt: string;
}

/**
 * GET /notifications response — the usual `data` + `pagination` envelope plus
 * `unreadCount` sitting OUTSIDE pagination so the bell badge is one number.
 * Mirrors NotificationListDto.
 */
export interface NotificationListDto {
  data: NotificationDto[];
  pagination: PaginationMeta;
  unreadCount: number;
}

export interface NotificationFilters {
  isRead?: boolean;
  page?: number;
  pageSize?: number;
}
