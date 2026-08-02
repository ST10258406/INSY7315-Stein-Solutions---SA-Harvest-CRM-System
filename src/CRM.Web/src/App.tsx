import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import LandingPage from './components/pages/LandingPage';
import { useHydrateAuth } from '@/features/auth/hooks/useHydrateAuth';
import { useAuthStore } from '@/store/authStore';
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
          <Route path="/" element={<LandingPage />} />
          {/* Future routes will go here */}
        </Routes>
      </div>
    </Router>
  );
}

export default App;
