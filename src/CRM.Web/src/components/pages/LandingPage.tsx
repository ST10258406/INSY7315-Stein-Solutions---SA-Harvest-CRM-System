import React, { useState } from 'react';
import LoginForm from '../auth/LoginForm';
import JwtDevModePanel from '../auth/JwtDevModePanel';

const LandingPage: React.FC = () => {
  const [devModeEnabled, setDevModeEnabled] = useState(false);
  const [jwtData, setJwtData] = useState<{ token: string; header: any; payload: any } | null>(null);

  const handleSimulatedLogin = (role: string, email: string) => {
    // Client-side mock JWT generation mimicking JwtTokenService
    const header = {
      alg: "HS256",
      typ: "JWT"
    };

    const payload = {
      sub: crypto.randomUUID(),
      email: email,
      given_name: "Demo",
      family_name: "User",
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + (60 * 60), // 60 minutes
      iss: "SAHarvestCRM",
      aud: "SAHarvestCRM.Client",
      role: role
    };

    const base64UrlEncode = (obj: any) => {
      return btoa(JSON.stringify(obj))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    };

    const encodedHeader = base64UrlEncode(header);
    const encodedPayload = base64UrlEncode(payload);
    const mockSignature = "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"; // Mock signature

    const token = `${encodedHeader}.${encodedPayload}.${mockSignature}`;
    
    setJwtData({ token, header, payload });
  };

  return (
    <div className="relative min-h-screen overflow-hidden bg-[#0A0A0A] font-sans selection:bg-primary/30">
      {/* Laravel-inspired background glow */}
      <div className="absolute top-0 -translate-y-12 left-1/2 -translate-x-1/2 w-[800px] h-[400px] bg-primary/20 blur-[120px] rounded-full pointer-events-none"></div>

      {/* Header */}
      <header className="relative z-10 flex items-center justify-between px-8 py-6 max-w-7xl mx-auto">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded bg-primary flex items-center justify-center font-bold text-primary-foreground">SA</div>
          <span className="text-xl font-semibold text-white tracking-tight">SA Harvest CRM</span>
        </div>
        
        <div className="flex items-center gap-4">
          <label className="flex items-center cursor-pointer gap-2">
            <span className="text-sm text-gray-400 font-medium">JWT Dev Mode</span>
            <div className="relative">
              <input 
                type="checkbox" 
                className="sr-only" 
                checked={devModeEnabled}
                onChange={(e) => setDevModeEnabled(e.target.checked)}
              />
              <div className={`block w-10 h-6 rounded-full transition-colors ${devModeEnabled ? 'bg-primary' : 'bg-gray-800 border border-gray-700'}`}></div>
              <div className={`dot absolute left-1 top-1 bg-white w-4 h-4 rounded-full transition-transform ${devModeEnabled ? 'translate-x-4' : ''}`}></div>
            </div>
          </label>
        </div>
      </header>

      {/* Main Content */}
      <main className="relative z-10 flex flex-col lg:flex-row items-center justify-center min-h-[calc(100vh-100px)] px-4 gap-12 max-w-7xl mx-auto pb-20">
        
        {/* Left Side: Hero Text */}
        <div className="flex-1 text-center lg:text-left">
          <h1 className="text-5xl lg:text-7xl font-bold text-white tracking-tighter mb-6 leading-tight">
            Nourishing our <br />
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary to-yellow-200">
              communities.
            </span>
          </h1>
          <p className="text-lg text-gray-400 mb-8 max-w-xl mx-auto lg:mx-0">
            Welcome to the SA Harvest Centralized Resource Management System. Empowering donors and staff to streamline logistics and fight hunger effectively.
          </p>
        </div>

        {/* Right Side: Interactive Panel (Form + Dev Mode) */}
        <div className={`flex gap-6 w-full max-w-md lg:max-w-none transition-all duration-500 ease-out ${devModeEnabled ? 'lg:w-[800px]' : 'lg:w-[450px]'}`}>
          
          <div className="flex-1 w-full max-w-[450px]">
            <LoginForm onSimulateLogin={handleSimulatedLogin} isDevMode={devModeEnabled} />
          </div>

          {devModeEnabled && (
            <div className="hidden lg:block flex-1 animate-in fade-in slide-in-from-right-8 duration-500">
              <JwtDevModePanel jwtData={jwtData} />
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default LandingPage;
