import {
  LayoutDashboard,
  Users,
  ClipboardList,
  CheckCircle2,
  BarChart3,
  UserCog,
  type LucideIcon,
} from 'lucide-react';
import { paths } from '@/routes/paths';

// Shared by TopBar and quick search (Issue 102) so both list the same pages.
export const ADMIN_ROLES = ['Admin', 'SuperAdmin'];

export interface NavItem {
  label: string;
  path: string;
  icon: LucideIcon;
  adminOnly?: boolean;
}

// `adminOnly` items (Approvals — Issue #113 — Reports — Issue #73 — and Users)
// are hidden from every non-Admin role. This is cosmetic only: the routes are
// `ProtectedRoute`-guarded and the APIs enforce `AdminOrAbove` server-side
// regardless of the nav.
export const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: paths.dashboard, icon: LayoutDashboard },
  { label: 'Donors', path: paths.donors, icon: Users },
  { label: 'Tasks', path: paths.tasks, icon: ClipboardList },
  { label: 'Approvals', path: paths.approvals, icon: CheckCircle2, adminOnly: true },
  { label: 'Reports', path: paths.reports, icon: BarChart3, adminOnly: true },
  { label: 'Users', path: paths.users, icon: UserCog, adminOnly: true },
];
