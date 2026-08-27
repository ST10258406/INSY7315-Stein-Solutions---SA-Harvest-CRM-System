import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import DashboardPage from './components/pages/DashboardPage';
import ApprovalsPage from './components/pages/ApprovalsPage';
import DonorsPage from './components/pages/DonorsPage';
import NewDonorPage from './components/pages/NewDonorPage';
import DonorDetailPage from './components/pages/DonorDetailPage';
import TasksPage from './components/pages/TasksPage';
import ReportsPage from './components/pages/ReportsPage';
import UsersPage from './components/pages/UsersPage';
import LoginPage from './features/auth/pages/LoginPage';
import ForgotPasswordPage from './features/auth/pages/ForgotPasswordPage';
import ResetPasswordPage from './features/auth/pages/ResetPasswordPage';
import { NotAuthorizedPage } from '@/features/auth/pages/NotAuthorizedPage';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { useHydrateAuth } from '@/features/auth/hooks/useHydrateAuth';
import { useAuthStore } from '@/store/authStore';
import { AppLayout } from '@/features/shell/AppLayout';
import { paths } from '@/routes/paths';
import './App.css';
import './lib/axios';

const queryClient = new QueryClient();

function App() {
  useHydrateAuth();
  const isHydrating = useAuthStore((s) => s.isHydrating);

  if (isHydrating) {
    return <div className="flex min-h-screen items-center justify-center dark bg-background text-foreground">Loading...</div>;
  }

  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <div className="dark bg-background text-foreground min-h-screen">
          <Routes>
            <Route path={paths.login} element={<LoginPage />} />
            <Route path={paths.forgotPassword} element={<ForgotPasswordPage />} />
            <Route path={paths.resetPassword} element={<ResetPasswordPage />} />

            {/* Authenticated app shell — every child below renders inside AppLayout (top bar + icon rail). */}
            <Route
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            >
              <Route path={paths.root} element={<Navigate to={paths.dashboard} replace />} />
              <Route path={paths.notAuthorized} element={<NotAuthorizedPage />} />
              <Route path={paths.dashboard} element={<DashboardPage />} />
              <Route path={paths.donors} element={<DonorsPage />} />
              <Route path={paths.donorNew} element={<NewDonorPage />} />
              <Route path={paths.donorDetailPattern} element={<DonorDetailPage />} />
              <Route path={paths.tasks} element={<TasksPage />} />
              <Route path={paths.reports} element={<ReportsPage />} />

              <Route
                path={paths.approvals}
                element={
                  <ProtectedRoute allowedRoles={["Admin", "SuperAdmin"]}>
                    <ApprovalsPage />
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
      </Router>
    </QueryClientProvider>
  );
}

export default App;
