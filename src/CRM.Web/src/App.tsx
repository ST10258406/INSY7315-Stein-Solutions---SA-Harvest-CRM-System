import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import LoginPage from './features/auth/pages/LoginPage';
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
            <Route path={paths.login} element={<LoginPage />} />
            <Route path="/" element={<Navigate to={paths.login} replace />} />
            <Route path={paths.dashboard} element={<div className="p-8">Dashboard Placeholder</div>} />
          </Routes>
        </div>
      </Router>
    </QueryClientProvider>
  );
}

export default App;
