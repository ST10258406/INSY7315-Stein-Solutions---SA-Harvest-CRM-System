import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import DashboardPage from './components/pages/DashboardPage';
import ApprovalsPage from './components/pages/ApprovalsPage';
import LoginPage from './features/auth/pages/LoginPage';
import ForgotPasswordPage from './features/auth/pages/ForgotPasswordPage';
import ResetPasswordPage from './features/auth/pages/ResetPasswordPage';
import { NotAuthorizedPage } from '@/features/auth/pages/NotAuthorizedPage';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { useHydrateAuth } from '@/features/auth/hooks/useHydrateAuth';
import { useAuthStore } from '@/store/authStore';
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
            <Route path="/" element={<Navigate to={paths.login} replace />} />
            <Route path={paths.login} element={<LoginPage />} />
            <Route path={paths.forgotPassword} element={<ForgotPasswordPage />} />
            <Route path={paths.resetPassword} element={<ResetPasswordPage />} />

            <Route
              path={paths.notAuthorized}
              element={
                <ProtectedRoute>
                  <NotAuthorizedPage />
                </ProtectedRoute>
              }
            />

            <Route
              path={paths.dashboard}
              element={
                <ProtectedRoute>
                  <DashboardPage />
                </ProtectedRoute>
              }
            />

            <Route
              path={paths.approvals}
              element={
                <ProtectedRoute allowedRoles={["Admin", "SuperAdmin"]}>
                  <ApprovalsPage />
                </ProtectedRoute>
              }
            />
          </Routes>
        </div>
      </Router>
    </QueryClientProvider>
  );
}

export default App;
