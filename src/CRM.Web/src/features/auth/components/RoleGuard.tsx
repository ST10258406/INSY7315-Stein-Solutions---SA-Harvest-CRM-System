import type { ReactNode } from 'react';
import { useAuthStore } from '@/store/authStore';

interface RoleGuardProps {
  allowedRoles: string[];
  children: ReactNode;
  fallback?: ReactNode; // what to render if not authorized — defaults to nothing
}

export function RoleGuard({ allowedRoles, children, fallback = null }: RoleGuardProps) {
  const userRoles = useAuthStore((s) => s.user?.roles) ?? [];

  const isAuthorized = userRoles.some((role) => allowedRoles.includes(role));

  if (!isAuthorized) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
