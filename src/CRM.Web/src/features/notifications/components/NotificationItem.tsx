import { formatRelativeTime } from '@/features/interactions/lib/interactionMeta';
import { NotificationTypeIcon } from './NotificationTypeIcon';
import { resolveNotificationHref } from '../lib/notificationMeta';
import type { NotificationDto } from '../types';

interface NotificationItemProps {
  notification: NotificationDto;
  onActivate: (notification: NotificationDto) => void;
}

export function NotificationItem({ notification, onActivate }: NotificationItemProps) {
  const hasTarget = resolveNotificationHref(notification) !== null;

  return (
    <button
      type="button"
      onClick={() => onActivate(notification)}
      className={`flex w-full items-start gap-3 border-b border-[var(--hair)] px-4 py-3 text-left transition-colors last:border-b-0 hover:bg-[var(--hover)] ${
        notification.isRead ? '' : 'bg-[var(--brand-yellow)]/[0.06]'
      }`}
    >
      <span
        className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-[10px] border ${
          notification.isRead
            ? 'border-[var(--border)] bg-[var(--icon-bg)] text-[var(--icon)]'
            : 'border-transparent bg-[var(--ink)] text-[var(--brand-yellow)]'
        }`}
      >
        <NotificationTypeIcon type={notification.notificationType} className="h-4 w-4" />
      </span>

      <span className="min-w-0 flex-1">
        <span className="flex items-baseline gap-2">
          <span
            className={`text-[13px] tracking-tight ${
              notification.isRead ? 'font-semibold text-[var(--muted-c)]' : 'font-extrabold text-[var(--ink)]'
            }`}
          >
            {notification.title}
          </span>
          <span className="ml-auto shrink-0 text-[11px] font-semibold text-[var(--muted2)]">
            {formatRelativeTime(notification.createdAt)}
          </span>
        </span>
        <span className="mt-0.5 line-clamp-2 block text-[12px] font-medium leading-snug text-[var(--muted-c)]">
          {notification.message}
        </span>
        {hasTarget && (
          <span className="mt-1 block text-[11px] font-bold text-[var(--ink)]/70">Open →</span>
        )}
      </span>

      {!notification.isRead && (
        <span className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-[var(--brand-red)]" aria-label="Unread" />
      )}
    </button>
  );
}
