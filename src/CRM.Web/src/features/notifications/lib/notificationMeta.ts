import { paths } from '@/routes/paths';
import type { NotificationDto } from '../types';

const LABELS: Record<string, string> = {
  FollowUpReminder: 'Follow-up reminder',
  NewDonorPendingReview: 'New donor pending review',
  TaskDue: 'Task due',
  TaskAssigned: 'Task assigned',
  DonorApproved: 'Donor approved',
  DonorRejected: 'Donor rejected',
};

export function notificationTypeLabel(type: string): string {
  return LABELS[type] ?? type;
}

/**
 * Where clicking a notification should take the user. `relatedEntityType` is a
 * loose string set by whichever handler created the row (e.g. "Donor",
 * "DonorTask") and `relatedEntityId` is that entity's own id. Only a donor has a
 * deep link; tasks/approvals fall back to their list pages.
 */
export function resolveNotificationHref(notification: NotificationDto): string | null {
  const entity = notification.relatedEntityType?.toLowerCase() ?? '';
  const id = notification.relatedEntityId;

  // Order matters: "DonorTask" contains "donor" but its id is a task id, so the
  // task check has to win before the loose donor-substring fallback.
  if (entity.includes('task')) return paths.tasks;
  if (entity.includes('approval')) return paths.approvals;
  if (entity.includes('donor') && id) return paths.donorDetail(id);

  // Fall back on the notification type when no entity type was recorded.
  switch (notification.notificationType) {
    case 'NewDonorPendingReview':
    case 'DonorApproved':
    case 'DonorRejected':
      return id ? paths.donorDetail(id) : paths.approvals;
    case 'TaskAssigned':
    case 'TaskDue':
      return paths.tasks;
    case 'FollowUpReminder':
      return id ? paths.donorDetail(id) : paths.donors;
    default:
      return null;
  }
}
