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

  // Still checking whether there's a valid session — don't decide anything yet.
  if (isHydrating) {
    return null; // or a full-page spinner component if the team has one
  }

  if (!isAuthenticated) {
    return <Navigate to={paths.login} state={{ from: location }} replace />;
  }

  if (allowedRoles && !userRoles.some((role) => allowedRoles.includes(role))) {
    return <Navigate to={paths.notAuthorized} replace />;
  }

  return <>{children}</>;
}
