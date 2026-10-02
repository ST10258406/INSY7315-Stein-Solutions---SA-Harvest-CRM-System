import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PublicFormPage from './features/publicForm/PublicFormPage';
import DashboardPage from './features/dashboard/DashboardPage';
import { ApprovalsPage } from './features/approvals';
import DonorListPage from './features/donors/DonorListPage';
import NewDonorPage from './components/pages/NewDonorPage';
import EditDonorPage from './components/pages/EditDonorPage';
import DonorDetailPage from './features/donors/pages/DonorDetailPage';
import { MyTasksPage } from './features/tasks';
import ReportsPage from './components/pages/ReportsPage';
import UsersPage from './components/pages/UsersPage';
import LoginPage from './features/auth/pages/LoginPage';
import ForgotPasswordPage from './features/auth/pages/ForgotPasswordPage';
import ResetPasswordPage from './features/auth/pages/ResetPasswordPage';
import ChangePasswordPage from './features/auth/pages/ChangePasswordPage';
import { NotAuthorizedPage } from '@/features/auth/pages/NotAuthorizedPage';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { useHydrateAuth } from '@/features/auth/hooks/useHydrateAuth';
import { useAuthStore } from '@/store/authStore';
import { AppLayout } from '@/features/shell/AppLayout';
import { Toaster } from '@/components/ui/sonner';
import { paths } from '@/routes/paths';
import '@/store/themeStore';
import './App.css';
import './lib/axios';

const queryClient = new QueryClient();

function App() {
  useHydrateAuth();
  const isHydrating = useAuthStore((s) => s.isHydrating);

  if (isHydrating) {
    return <div className="flex min-h-screen items-center justify-center bg-background text-foreground">Loading...</div>;
  }

  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <div className="bg-background text-foreground min-h-screen">
          <Routes>
            {/* No landing page: the root goes straight to Login. */}
            <Route path={paths.root} element={<Navigate to={paths.login} replace />} />
            {/* Public donor onboarding form — reached externally with zero auth,
                so it renders standalone, outside AppLayout. */}
            <Route path={paths.publicDonorForm} element={<PublicFormPage />} />
            <Route path={paths.login} element={<LoginPage />} />
            <Route path={paths.forgotPassword} element={<ForgotPasswordPage />} />
            <Route path={paths.resetPassword} element={<ResetPasswordPage />} />

            {/* Forced password change — authenticated but deliberately outside the app shell,
                so a user on a temporary password has no navigation to wander off to. */}
            <Route
              path={paths.changePassword}
              element={
                <ProtectedRoute>
                  <ChangePasswordPage />
                </ProtectedRoute>
              }
            />

            {/* Authenticated app shell — every child below renders inside AppLayout (top bar + icon rail). */}
            <Route
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            >
              <Route path={paths.notAuthorized} element={<NotAuthorizedPage />} />
              <Route path={paths.dashboard} element={<DashboardPage />} />
              <Route path={paths.donors} element={<DonorListPage />} />
              <Route path={paths.donorNew} element={<NewDonorPage />} />
              <Route path={paths.donorEditPattern} element={<EditDonorPage />} />
              <Route path={paths.donorDetailPattern} element={<DonorDetailPage />} />
              <Route path={paths.tasks} element={<MyTasksPage />} />

              <Route
                path={paths.approvals}
                element={
                  <ProtectedRoute allowedRoles={["Admin", "SuperAdmin"]}>
                    <ApprovalsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path={paths.reports}
                element={
                  <ProtectedRoute allowedRoles={["Admin", "SuperAdmin"]}>
                    <ReportsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path={paths.users}
                element={
                  <ProtectedRoute allowedRoles={["Admin", "SuperAdmin"]}>
                    <UsersPage />
                  </ProtectedRoute>
                }
              />
            </Route>
          </Routes>
        </div>
        <Toaster />
      </Router>
    </QueryClientProvider>
  );
}

export default App;
