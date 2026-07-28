import React, { useState } from 'react';
import LoginForm from '../auth/LoginForm';
import JwtDevModePanel from '../auth/JwtDevModePanel';
import { loginApi, decodeJwtToken, type LoginResponseDto } from '../../services/authService';

const LandingPage: React.FC = () => {
  const [devModeEnabled, setDevModeEnabled] = useState(false);
  const [jwtData, setJwtData] = useState<{
    token: string;
    header: Record<string, unknown>;
    payload: Record<string, unknown>;
    isRealApi?: boolean;
  } | null>(null);
  const [userSession, setUserSession] = useState<LoginResponseDto | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);

  const handleRealLogin = async (email: string, password: string) => {
    setApiError(null);
    try {
      const response = await loginApi(email, password);
      setUserSession(response);

      // Decode the real JWT issued by CRM.API for inspector display
      const decoded = decodeJwtToken(response.accessToken);
      setJwtData({
        token: response.accessToken,
        header: decoded.header,
        payload: decoded.payload,
        isRealApi: true
      });

      // Save session in localStorage for client state persistence
      localStorage.setItem('crm_access_token', response.accessToken);
      localStorage.setItem('crm_user', JSON.stringify(response.user));
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Login failed. Please check your credentials or API connection.';
      setApiError(msg);
      throw err;
    }
  };

  const handleSimulatedLogin = (role: string, email: string) => {
    setApiError(null);
    const header = {
      alg: "HS256",
      typ: "JWT"
    };

    const payload = {
      sub: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : '00000000-0000-0000-0000-000000000000',
      email: email,
      given_name: "Demo",
      family_name: "User",
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + (60 * 60), // 60 minutes
      iss: "SAHarvestCRM",
      aud: "SAHarvestCRM.Client",
      role: role
    };

    const base64UrlEncode = (obj: Record<string, unknown>) => {
      return btoa(JSON.stringify(obj))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    };

    const encodedHeader = base64UrlEncode(header);
    const encodedPayload = base64UrlEncode(payload);
    const mockSignature = "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
    const token = `${encodedHeader}.${encodedPayload}.${mockSignature}`;

    setJwtData({ token, header, payload, isRealApi: false });
  };

  const handleLogout = () => {
    setUserSession(null);
    setJwtData(null);
    localStorage.removeItem('crm_access_token');
    localStorage.removeItem('crm_user');
  };

  return (
    <div className="relative min-h-screen overflow-hidden bg-[#0A0A0A] font-sans selection:bg-primary/30">
      {/* Background glow */}
      <div className="absolute top-0 -translate-y-12 left-1/2 -translate-x-1/2 w-[800px] h-[400px] bg-primary/20 blur-[120px] rounded-full pointer-events-none"></div>

      {/* Header */}
      <header className="relative z-10 flex items-center justify-between px-8 py-6 max-w-7xl mx-auto">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded bg-primary flex items-center justify-center font-bold text-primary-foreground">SA</div>
          <span className="text-xl font-semibold text-white tracking-tight">SA Harvest CRM</span>
        </div>

        <div className="flex items-center gap-6">
          <div className="text-xs text-emerald-400 font-mono bg-emerald-950/40 border border-emerald-800/50 px-3 py-1.5 rounded-full flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            Strict REST API Mode (No DB Access)
          </div>

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

        {/* Right Side: Interactive Auth Panel */}
        <div className={`flex gap-6 w-full max-w-md lg:max-w-none transition-all duration-500 ease-out ${devModeEnabled ? 'lg:w-[800px]' : 'lg:w-[450px]'}`}>

          <div className="flex-1 w-full max-w-[450px]">
            {userSession ? (
              /* Logged In Active Session Card */
              <div className="bg-[#121212]/80 backdrop-blur-xl border border-emerald-800/50 rounded-2xl shadow-2xl p-8 w-full">
                <div className="flex items-center gap-3 mb-6 pb-4 border-b border-gray-800">
                  <div className="w-12 h-12 rounded-full bg-emerald-500/20 border border-emerald-500/40 text-emerald-400 flex items-center justify-center font-bold text-lg">
                    {userSession.user.firstName?.[0] || 'U'}{userSession.user.lastName?.[0] || ''}
                  </div>
                  <div>
                    <h3 className="text-lg font-semibold text-white">
                      {userSession.user.firstName} {userSession.user.lastName}
                    </h3>
                    <p className="text-xs text-gray-400">{userSession.user.email}</p>
                  </div>
                </div>

                <div className="space-y-3 mb-6 text-sm">
                  <div className="flex justify-between py-1.5 border-b border-gray-800/50">
                    <span className="text-gray-400">Assigned Roles</span>
                    <span className="text-emerald-400 font-medium">{userSession.user.roles.join(', ') || 'User'}</span>
                  </div>
                  <div className="flex justify-between py-1.5 border-b border-gray-800/50">
                    <span className="text-gray-400">JWT Token Status</span>
                    <span className="text-xs font-mono bg-emerald-950 text-emerald-300 px-2 py-0.5 rounded border border-emerald-800">Valid</span>
                  </div>
                  <div className="flex justify-between py-1.5">
                    <span className="text-gray-400">Expires In</span>
                    <span className="text-gray-300 font-mono">{userSession.expiresIn}s</span>
                  </div>
                </div>

                <button
                  onClick={handleLogout}
                  className="w-full py-3 px-4 bg-gray-800 hover:bg-gray-700 text-gray-200 font-medium rounded-lg transition-colors"
                >
                  Sign Out Session
                </button>
              </div>
            ) : (
              <LoginForm
                onRealLogin={handleRealLogin}
                onSimulateLogin={handleSimulatedLogin}
                isDevMode={devModeEnabled}
                apiError={apiError}
              />
            )}
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
