import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  ClipboardList,
  CheckCircle2,
  BarChart3,
  UserCog,
  ShieldCheck,
  ChevronDown,
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
import { useVettingSubmissions } from '@/features/vetting/hooks';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import logoImg from '@/assets/sa-harvest-logo.png';

const ADMIN_ROLES = ['Admin', 'SuperAdmin'];
const PROCUREMENT_OR_ABOVE = ['Procurement', ...ADMIN_ROLES];

interface SubLink {
  label: string;
  path: string;
  adminOnly?: boolean;
  /** Shows how many vettings are waiting for review. */
  showReviewCount?: boolean;
}

interface NavItem {
  label: string;
  path: string;
  icon: LucideIcon;
  adminOnly?: boolean;
  procurementOrAbove?: boolean;
  /** Renders the item as a dropdown of these links instead of a single link. */
  children?: SubLink[];
}

// `procurementOrAbove` items (Vetting) are hidden from Marketing; `adminOnly` items (Approvals — Issue #113 — Reports — Issue #73 — and Users)
// are hidden from every non-Admin role. This is cosmetic only: the routes are
// `ProtectedRoute`-guarded and the APIs enforce `AdminOrAbove` server-side
// regardless of the nav.
const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: paths.dashboard, icon: LayoutDashboard },
  { label: 'Donors', path: paths.donors, icon: Users },
  { label: 'Tasks', path: paths.tasks, icon: ClipboardList },
  {
    label: 'Vetting',
    path: paths.vetting,
    icon: ShieldCheck,
    procurementOrAbove: true,
    children: [
      { label: 'Submissions', path: paths.vetting },
      { label: 'Site visits', path: paths.vettingVisits },
      { label: 'Review queue', path: paths.vettingReview, adminOnly: true, showReviewCount: true },
      { label: 'Officers', path: paths.vettingOfficers, adminOnly: true },
    ],
  },
  { label: 'Approvals', path: paths.approvals, icon: CheckCircle2, adminOnly: true },
  { label: 'Reports', path: paths.reports, icon: BarChart3, adminOnly: true },
  { label: 'Users', path: paths.users, icon: UserCog, adminOnly: true },
];

function isRouteActive(pathname: string, itemPath: string) {
  return pathname === itemPath || pathname.startsWith(`${itemPath}/`);
}

function pillClass(isActive: boolean) {
  return `flex h-10 items-center gap-2 rounded-full border pl-2 pr-4 text-[13.5px] font-semibold whitespace-nowrap transition-colors ${
    isActive
      ? 'border-pill-active-bg bg-pill-active-bg text-pill-active-fg font-bold'
      : 'border-border bg-pill text-pill-ink hover:border-foreground'
  }`;
}

function PillIcon({ icon: Icon, isActive }: { icon: LucideIcon; isActive: boolean }) {
  return (
    <span
      className={`flex h-6 w-6 items-center justify-center rounded-full ${
        isActive ? 'bg-brand/20 text-brand' : 'bg-pill-icon-bg text-icon'
      }`}
    >
      <Icon className="h-4 w-4" />
    </span>
  );
}

export function TopBar() {
  const location = useLocation();
  const user = useAuthStore((s) => s.user);
  const roles = user?.roles ?? [];
  const isAdmin = roles.some((role) => ADMIN_ROLES.includes(role));
  const isProcurementOrAbove = roles.some((role) => PROCUREMENT_OR_ABOVE.includes(role));
  // Reuses the list endpoint's total (pageSize=1) rather than a dedicated count endpoint. Admins only.
  const { data: readyPage } = useVettingSubmissions({ approvalState: 'Ready', pageSize: 1 }, { enabled: isAdmin });
  const reviewCount = readyPage?.pagination?.totalCount ?? 0;
  const { mutate: logout, isPending: isLoggingOut } = useLogout();
  const theme = useThemeStore((s) => s.theme);
  const toggleTheme = useThemeStore((s) => s.toggleTheme);

  // Filtered before render — Admin-only items never reach the DOM for other roles.
  const visibleNavItems = NAV_ITEMS.filter(
    (item) => (!item.adminOnly || isAdmin) && (!item.procurementOrAbove || isProcurementOrAbove),
  );

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

          if (item.children) {
            return (
              <DropdownMenu key={item.path}>
                <DropdownMenuTrigger className={pillClass(isActive)}>
                  <PillIcon icon={item.icon} isActive={isActive} />
                  <span>{item.label}</span>
                  {reviewCount > 0 && item.children.some((c) => c.showReviewCount) && isAdmin && (
                    <span className="rounded-full bg-destructive px-1.5 text-[10.5px] font-bold text-white" aria-label={`${reviewCount} waiting for review`}>
                      {reviewCount}
                    </span>
                  )}
                  <ChevronDown aria-hidden className="h-3.5 w-3.5" />
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-auto min-w-44 p-1.5">
                  {item.children
                    .filter((child) => !child.adminOnly || isAdmin)
                    .map((child) => (
                      <DropdownMenuItem
                        key={child.path}
                        render={<Link to={child.path} />}
                        className="text-[13px] font-semibold"
                      >
                        {child.label}
                        {child.showReviewCount && reviewCount > 0 && (
                          <span className="ml-auto rounded-full bg-destructive/10 px-1.5 text-[11px] font-bold text-destructive">{reviewCount}</span>
                        )}
                      </DropdownMenuItem>
                    ))}
                </DropdownMenuContent>
              </DropdownMenu>
            );
          }

          return (
            <Link
              key={item.path}
              to={item.path}
              aria-current={isActive ? 'page' : undefined}
              className={pillClass(isActive)}
            >
              <PillIcon icon={item.icon} isActive={isActive} />
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
