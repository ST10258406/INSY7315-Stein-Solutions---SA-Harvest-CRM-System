import { useEffect, useState } from 'react';
import { Bell } from 'lucide-react';
import { useNotifications } from '../hooks';
import { NotificationPanel } from './NotificationPanel';

/** Header bell + unread badge + dropdown panel. Rendered in TopBar. */
export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const { data } = useNotifications({ pageSize: 12 });
  const unreadCount = data?.unreadCount ?? 0;
  const badge = unreadCount > 99 ? '99+' : String(unreadCount);

  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false);
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open]);

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={unreadCount > 0 ? `Notifications, ${unreadCount} unread` : 'Notifications'}
        aria-expanded={open}
        className="relative flex h-9.5 w-9.5 items-center justify-center rounded-full border border-border bg-card text-icon transition-colors hover:border-foreground"
      >
        <Bell className="h-4 w-4" />
        {unreadCount > 0 && (
          <span className="absolute -right-1 -top-1 flex h-[17px] min-w-[17px] items-center justify-center rounded-full border-2 border-background bg-[var(--ink)] px-1 text-[9.5px] font-bold text-[var(--brand-yellow)]">
            {badge}
          </span>
        )}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} aria-hidden />
          <div className="absolute right-0 top-[46px] z-50">
            <NotificationPanel onClose={() => setOpen(false)} />
          </div>
        </>
      )}
    </div>
  );
}
