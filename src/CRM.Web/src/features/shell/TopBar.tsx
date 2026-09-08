import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  ClipboardList,
  CheckCircle2,
  BarChart3,
  UserCog,
  LogOut,
  Sun,
  Moon,
  type LucideIcon,
} from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { useThemeStore } from '@/store/themeStore';
import { useLogout } from '@/features/auth/hooks/useLogout';
import { NotificationBell } from '@/features/notifications';
import { paths } from '@/routes/paths';
import logoImg from '@/assets/sa-harvest-logo.png';

const ADMIN_ROLES = ['Admin', 'SuperAdmin'];

interface NavItem {
  label: string;
  path: string;
  icon: LucideIcon;
  adminOnly?: boolean;
}

// `adminOnly` items (Approvals — Issue #113 — and Users) are hidden from every
// non-Admin role. This is cosmetic only: the routes are `ProtectedRoute`-guarded
// and the APIs enforce `AdminOrAbove` server-side regardless of the nav.
const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: paths.dashboard, icon: LayoutDashboard },
  { label: 'Donors', path: paths.donors, icon: Users },
  { label: 'Tasks', path: paths.tasks, icon: ClipboardList },
  { label: 'Approvals', path: paths.approvals, icon: CheckCircle2, adminOnly: true },
  { label: 'Reports', path: paths.reports, icon: BarChart3 },
  { label: 'Users', path: paths.users, icon: UserCog, adminOnly: true },
];

function isRouteActive(pathname: string, itemPath: string) {
  return pathname === itemPath || pathname.startsWith(`${itemPath}/`);
}

export function TopBar() {
  const location = useLocation();
  const user = useAuthStore((s) => s.user);
  const roles = user?.roles ?? [];
  const isAdmin = roles.some((role) => ADMIN_ROLES.includes(role));
  const { mutate: logout, isPending: isLoggingOut } = useLogout();
  const theme = useThemeStore((s) => s.theme);
  const toggleTheme = useThemeStore((s) => s.toggleTheme);

  // Filtered before render — Admin-only items never reach the DOM for other roles.
  const visibleNavItems = NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin);

  const initials = user ? `${user.firstName[0] ?? ''}${user.lastName[0] ?? ''}`.toUpperCase() : '';

  return (
    <header className="flex h-[88px] shrink-0 items-center gap-7 border-b border-border bg-background px-6.5">
      {/* Brand */}
      <Link to={paths.dashboard} className="flex shrink-0 items-center gap-3">
        <img
          src={logoImg}
          alt="S.A. Harvest"
          className="h-[46px] w-[46px] rounded-2xl border border-border object-cover"
        />
        <div className="flex flex-col leading-snug">
          <span className="text-[15.5px] font-extrabold tracking-tight text-foreground">SA Harvest</span>
          <span className="text-[9.5px] font-bold tracking-[1.4px] text-muted-foreground">DONOR CRM</span>
        </div>
      </Link>

      {/* Primary navigation */}
      <nav className="flex min-w-0 items-center gap-2 overflow-x-auto">
        {visibleNavItems.map((item) => {
          const isActive = isRouteActive(location.pathname, item.path);
          const Icon = item.icon;
          return (
            <Link
              key={item.path}
              to={item.path}
              aria-current={isActive ? 'page' : undefined}
              className={`flex h-10 items-center gap-2 rounded-full border pl-2 pr-4 text-[13.5px] font-semibold whitespace-nowrap transition-colors ${
                isActive
                  ? 'border-pill-active-bg bg-pill-active-bg text-pill-active-fg font-bold'
                  : 'border-border bg-pill text-pill-ink hover:border-foreground'
              }`}
            >
              <span
                className={`flex h-6 w-6 items-center justify-center rounded-full ${
                  isActive ? 'bg-brand/20 text-brand' : 'bg-pill-icon-bg text-icon'
                }`}
              >
                <Icon className="h-4 w-4" />
              </span>
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>

      {/* User controls */}
      <div className="ml-auto flex shrink-0 items-center gap-3">
        <button
          type="button"
          onClick={toggleTheme}
          title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          className="flex h-9.5 w-9.5 items-center justify-center rounded-full border border-border bg-card text-icon transition-colors hover:border-foreground"
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </button>

        <NotificationBell />

        <div className="flex items-center gap-2.5 pl-1.5">
          <div className="flex h-[38px] w-[38px] items-center justify-center rounded-full border-2 border-border bg-secondary text-xs font-bold text-foreground">
            {initials}
          </div>
          <div className="hidden flex-col leading-tight sm:flex">
            <span className="text-[13px] font-semibold text-foreground">
              {user ? `${user.firstName} ${user.lastName}` : ''}
            </span>
            <span className="text-[10.5px] text-muted-foreground">{roles[0]}</span>
          </div>
        </div>

        <button
          type="button"
          onClick={() => logout()}
          disabled={isLoggingOut}
          title="Log out"
          className="flex h-9.5 w-9.5 items-center justify-center rounded-full border border-border bg-card text-icon transition-colors hover:border-foreground disabled:opacity-50"
        >
          <LogOut className="h-4 w-4" />
        </button>
      </div>
    </header>
  );
}
