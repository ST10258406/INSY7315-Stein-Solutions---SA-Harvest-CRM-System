import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import LandingPage from './components/pages/LandingPage';
import DashboardPage from './components/pages/DashboardPage';
import ApprovalsPage from './components/pages/ApprovalsPage';
import { NotAuthorizedPage } from '@/features/auth/pages/NotAuthorizedPage';
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute';
import { useHydrateAuth } from '@/features/auth/hooks/useHydrateAuth';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';
import './App.css'; // Might need to keep or replace
import './lib/axios'; // Ensure axios interceptors are registered

function App() {
  useHydrateAuth();
  const isHydrating = useAuthStore((s) => s.isHydrating);

  if (isHydrating) {
    // Prevent flashing a "logged out" state before checking the token
    return <div className="flex min-h-screen items-center justify-center dark bg-background text-foreground">Loading...</div>;
  }

  return (
    <Router>
      <div className="dark bg-background text-foreground min-h-screen">
        <Routes>
          <Route path={paths.login} element={<LandingPage />} />
          
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
              <ProtectedRoute allowedRoles={['Admin', 'SuperAdmin']}>
                <ApprovalsPage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
