import { useNavigate } from 'react-router-dom';
import { BellOff, TriangleAlert, CheckCheck } from 'lucide-react';
import { useNotifications, useMarkNotificationRead, useMarkAllNotificationsRead } from '../hooks';
import { NotificationItem } from './NotificationItem';
import { resolveNotificationHref } from '../lib/notificationMeta';
import type { NotificationDto } from '../types';

interface NotificationPanelProps {
  onClose: () => void;
}

export function NotificationPanel({ onClose }: NotificationPanelProps) {
  const navigate = useNavigate();
  const { data, isPending, isError, refetch } = useNotifications({ pageSize: 12 });
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const notifications = data?.data ?? [];
  const unreadCount = data?.unreadCount ?? 0;

  const activate = (notification: NotificationDto) => {
    if (!notification.isRead) markRead.mutate(notification.id);
    const href = resolveNotificationHref(notification);
    onClose();
    if (href) navigate(href);
  };

  return (
    <div className="flex max-h-[70vh] w-[360px] max-w-[calc(100vw-24px)] flex-col overflow-hidden rounded-2xl border border-[var(--border)] bg-[var(--card)] shadow-[0_16px_44px_rgba(20,20,15,0.22)]">
      <div className="flex items-center justify-between gap-3 border-b border-[var(--border)] px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="text-[13.5px] font-extrabold tracking-tight text-[var(--ink)]">Notifications</span>
          {unreadCount > 0 && (
            <span className="inline-flex h-4.5 min-w-[18px] items-center justify-center rounded-full bg-[var(--ink)] px-1.5 text-[10px] font-bold text-[var(--brand-yellow)]">
              {unreadCount}
            </span>
          )}
        </div>
        <button
          type="button"
          onClick={() => markAllRead.mutate()}
          disabled={unreadCount === 0 || markAllRead.isPending}
          className="flex items-center gap-1.5 text-[12px] font-bold text-[var(--ink)] transition-opacity hover:opacity-70 disabled:opacity-40"
        >
          <CheckCheck className="h-3.5 w-3.5" />
          <span>Mark all read</span>
        </button>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto">
        {isPending ? (
          <div className="flex flex-col gap-2 p-4" aria-hidden>
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-14 animate-pulse rounded-xl bg-muted" />
            ))}
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 px-6 py-10 text-center">
            <TriangleAlert className="h-5 w-5 text-muted-foreground" />
            <p className="m-0 text-[13px] font-semibold text-foreground">Couldn't load notifications</p>
            <button
              type="button"
              onClick={() => refetch()}
              className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
            >
              Retry
            </button>
          </div>
        ) : notifications.length === 0 ? (
          <div className="flex flex-col items-center gap-2.5 px-6 py-12 text-center">
            <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-icon-bg">
              <BellOff className="h-5 w-5 text-icon" />
            </span>
            <p className="m-0 text-[13px] font-bold tracking-tight text-foreground">You're all caught up</p>
            <p className="m-0 text-[11.5px] font-medium text-muted-foreground">
              New follow-ups, tasks and approvals will show up here.
            </p>
          </div>
        ) : (
          notifications.map((notification) => (
            <NotificationItem key={notification.id} notification={notification} onActivate={activate} />
          ))
        )}
      </div>
    </div>
  );
}
