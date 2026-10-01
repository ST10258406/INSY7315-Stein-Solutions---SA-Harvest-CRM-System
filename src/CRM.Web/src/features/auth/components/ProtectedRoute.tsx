import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';

interface ProtectedRouteProps {
  children: ReactNode;
  allowedRoles?: string[]; // omit to mean "any authenticated user, any role"
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const location = useLocation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const isHydrating = useAuthStore((s) => s.isHydrating);
  const userRoles = useAuthStore((s) => s.user?.roles) ?? [];
  const mustChangePassword = useAuthStore((s) => s.user?.mustChangePassword === true);

  // Still checking whether there's a valid session — don't decide anything yet.
  if (isHydrating) {
    return null; // or a full-page spinner component if the team has one
  }

  // Access tokens are memory-only (see authStore) — any full page refresh
  // clears them, so this fires on every reload of a protected route, not
  // just a first visit. Landing on the marketing/entry page here (rather
  // than a bare /login form) is deliberate: it's the front door back in.
  if (!isAuthenticated) {
    return <Navigate to={paths.root} state={{ from: location }} replace />;
  }

  // Seeded/temporary password: nothing else is reachable until it is changed. The API enforces
  // this too (403 PASSWORD_CHANGE_REQUIRED); this redirect is just the UX for it.
  if (mustChangePassword && location.pathname !== paths.changePassword) {
    return <Navigate to={paths.changePassword} replace />;
  }

  if (allowedRoles &&!userRoles.some((role) => allowedRoles.includes(role))) {
    return <Navigate to={paths.notAuthorized} replace />;
  }

  return <>{children}</>;
}
