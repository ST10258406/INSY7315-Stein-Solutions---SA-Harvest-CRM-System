import {
  LayoutDashboard,
  Users,
  ClipboardList,
  CheckCircle2,
  BarChart3,
  UserCog,
  ShieldCheck,
  type LucideIcon,
} from 'lucide-react';
import { paths } from '@/routes/paths';

// Shared by TopBar and quick search (Issue 102) so both list the same pages.
export const ADMIN_ROLES = ['Admin', 'SuperAdmin'];
export const PROCUREMENT_OR_ABOVE = ['Procurement', ...ADMIN_ROLES];

export interface SubLink {
  label: string;
  path: string;
  adminOnly?: boolean;
  /** Shows how many vettings are waiting for review. */
  showReviewCount?: boolean;
}

export interface NavItem {
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
export const NAV_ITEMS: NavItem[] = [
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
