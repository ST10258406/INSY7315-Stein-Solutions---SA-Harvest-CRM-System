import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import LandingPage from './components/pages/LandingPage';
import './App.css'; // Might need to keep or replace
import './lib/axios'; // Ensure axios instance runs and attaches to window

function App() {
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
