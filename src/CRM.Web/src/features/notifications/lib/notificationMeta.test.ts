import { describe, it, expect } from 'vitest';
import { resolveNotificationHref, notificationTypeLabel } from './notificationMeta';
import type { NotificationDto } from '../types';

function notif(overrides: Partial<NotificationDto> = {}): NotificationDto {
  return {
    id: 'n1',
    title: 't',
    message: 'm',
    isRead: false,
    readAt: null,
    notificationType: 'TaskAssigned',
    relatedEntityId: null,
    relatedEntityType: null,
    createdAt: '2026-09-06T09:00:00Z',
    ...overrides,
  };
}

describe('resolveNotificationHref', () => {
  it('deep-links a donor-related notification to the donor detail page', () => {
    expect(
      resolveNotificationHref(notif({ relatedEntityType: 'Donor', relatedEntityId: 'donor-9' })),
    ).toBe('/donors/donor-9');
  });

  it('sends task notifications to the tasks page', () => {
    expect(resolveNotificationHref(notif({ notificationType: 'TaskAssigned' }))).toBe('/tasks');
    expect(resolveNotificationHref(notif({ relatedEntityType: 'DonorTask' }))).toBe('/tasks');
  });

  it('routes a DonorTask notification to /tasks even though its type string contains "donor"', () => {
    // relatedEntityId here is a TASK id — must NOT be treated as a donor id.
    expect(
      resolveNotificationHref(
        notif({ notificationType: 'TaskAssigned', relatedEntityType: 'DonorTask', relatedEntityId: 'task-7' }),
      ),
    ).toBe('/tasks');
  });

  it('sends approval notifications without a donor id to the approvals page', () => {
    expect(resolveNotificationHref(notif({ notificationType: 'NewDonorPendingReview' }))).toBe('/approvals');
  });

  it('returns null when nothing maps', () => {
    expect(resolveNotificationHref(notif({ notificationType: 'Unknown' as never }))).toBeNull();
  });
});

describe('notificationTypeLabel', () => {
  it('humanises known types and echoes unknown ones', () => {
    expect(notificationTypeLabel('DonorApproved')).toBe('Donor approved');
    expect(notificationTypeLabel('Whatever')).toBe('Whatever');
  });
});
