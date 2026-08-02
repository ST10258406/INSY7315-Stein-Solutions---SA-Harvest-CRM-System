import React, { useState } from 'react';
import LoginForm from '@/components/auth/LoginForm';
import { loginApi, type LoginResponseDto } from '@/services/authService';
import { useAuthStore } from '@/store/authStore';

const TONNES_RESCUED_THIS_MONTH = 124;

function getTallyMarks(tonnes: number) {
  const scale = Math.max(1, Math.round(tonnes / 22));
  const strokes = Math.round(tonnes / scale);
  const note = scale === 1 ? 'Each mark is a tonne.' : `Each mark is ${scale} tonnes.`;
  return {
    bundles: Array.from({ length: Math.floor(strokes / 5) }),
    remainder: Array.from({ length: strokes % 5 }),
    note,
  };
}

const LandingPage: React.FC = () => {
  const [userSession, setUserSession] = useState<LoginResponseDto | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const { bundles, remainder, note } = getTallyMarks(TONNES_RESCUED_THIS_MONTH);

  const handleLogin = async (email: string, password: string) => {
    setApiError(null);
    try {
      const response = await loginApi(email, password);
      setUserSession(response);
      useAuthStore.getState().login(response);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Login failed. Please check your credentials or API connection.';
      setApiError(msg);
      throw err;
    }
  };

  const handleLogout = () => {
    setUserSession(null);
    setApiError(null);
    useAuthStore.getState().logout();
  };

  return (
    <div
      className="flex h-screen w-full overflow-hidden bg-[#F4F2EC] text-[#17140F]"
      style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}
    >
      {/* Brand panel */}
      <div className="hidden lg:flex w-[44%] min-w-[420px] flex-none flex-col justify-between bg-[#17140F] p-11 relative overflow-hidden">
        <div className="absolute top-0 left-0 w-full h-1 bg-[#F2B705]" />

        <div className="flex items-center gap-[11px]">
          <div className="w-9 h-9 rounded-[10px] bg-[#F2B705] flex items-center justify-center flex-none">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <path d="M12 2C12 8 8 9 8 14C8 17.3 9.8 20 12 22C14.2 20 16 17.3 16 14C16 9 12 8 12 2Z" fill="#17140F" />
              <path d="M12 12V22" stroke="#F2B705" strokeWidth={1.4} />
            </svg>
          </div>
          <div className="leading-tight">
            <div className="font-bold text-[15px] text-[#FBF7EE]">SA Harvest</div>
            <div className="text-[11px] text-[#8C8578] font-medium">Donor CRM</div>
          </div>
        </div>

        <div>
          <div className="text-[10px] font-bold tracking-wide text-[#F2B705] uppercase mb-3.5">Rescued this month</div>
          <div className="flex items-end gap-[9px] min-h-[38px] mb-3.5">
            {bundles.map((_, i) => (
              <div key={`bundle-${i}`} className="relative w-[22px] h-[38px] flex-none">
                <div className="absolute left-px top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
                <div className="absolute left-[7px] top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
                <div className="absolute left-[13px] top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
                <div className="absolute left-[19px] top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
                <div className="absolute -left-0.5 top-4 w-[26px] h-[2.5px] bg-[#F2B705] rounded-[1px] -rotate-[30deg]" />
              </div>
            ))}
            {remainder.map((_, i) => (
              <div key={`remainder-${i}`} className="w-[2.5px] h-[38px] bg-[#F4F2EC] flex-none rounded-[1px]" />
            ))}
          </div>
          <div className="text-[31px] font-bold text-[#FBF7EE] leading-tight max-w-[400px]">
            {TONNES_RESCUED_THIS_MONTH} tonnes of surplus food moved to communities that needed it.
          </div>
          <div className="text-[13px] text-[#8C8578] mt-3 max-w-[380px]">
            {note} Every donor relationship you keep warm in here is part of that number.
          </div>
        </div>

        <div className="flex items-center gap-[18px] text-[11.5px] text-[#6E6857]">
          <span>© 2026 SA Harvest NPC</span>
          <a href="#" className="text-[#8C8578] hover:text-[#B65C36]">Privacy</a>
          <a href="#" className="text-[#8C8578] hover:text-[#B65C36]">Support</a>
        </div>
      </div>

      {/* Form panel */}
      <div className="flex-1 flex items-center justify-center p-8 overflow-y-auto">
        <div className="w-full max-w-[396px]">
          {userSession ? (
            <>
              <div className="text-[23px] font-bold mb-1">Signed in</div>
              <div className="text-[13px] text-[#8A8374] mb-[22px]">
                You're in. The full CRM workspace lands here once it's connected.
              </div>

              <div className="bg-white border border-[#E4DECE] rounded-xl p-[22px]">
                <div className="flex items-center gap-3 mb-5 pb-4 border-b border-[#EDE7D9]">
                  <div className="w-11 h-11 rounded-full bg-[#F2B705]/20 border border-[#F2B705]/50 text-[#8A5A00] flex items-center justify-center font-bold flex-none">
                    {userSession.user.firstName?.[0] || 'U'}{userSession.user.lastName?.[0] || ''}
                  </div>
                  <div>
                    <div className="text-[15px] font-semibold text-[#17140F]">
                      {userSession.user.firstName} {userSession.user.lastName}
                    </div>
                    <div className="text-xs text-[#8A8374]">{userSession.user.email}</div>
                  </div>
                </div>
                <div className="text-sm text-[#3F3A2E] mb-5">
                  Roles: <span className="font-medium">{userSession.user.roles.join(', ') || 'User'}</span>
                </div>
                <button
                  onClick={handleLogout}
                  className="w-full bg-[#F4F2EC] hover:bg-[#EDE7D9] text-[#3F3A2E] font-semibold rounded-lg py-2.5 transition-colors"
                >
                  Sign out
                </button>
              </div>
            </>
          ) : (
            <>
              <div className="text-[23px] font-bold mb-1">Sign in</div>
              <div className="text-[13px] text-[#8A8374] mb-[22px]">Use your SA Harvest staff account.</div>

              <LoginForm onLogin={handleLogin} apiError={apiError} />

              <div className="flex items-start gap-[9px] mt-3.5 bg-[#EFECFA] border border-[#DCD6F3] rounded-[9px] px-[13px] py-2.5">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#4E4483" strokeWidth={2} className="flex-none mt-px">
                  <rect x="4" y="10" width="16" height="10" rx="2" />
                  <path d="M8 10V7.5a4 4 0 0 1 8 0V10" />
                </svg>
                <div className="text-[11.5px] text-[#3B3560] leading-relaxed">
                  Accounts are provisioned by an admin — your role (Marketing, Procurement or Admin) decides what you can see and edit. Need access? <a href="#" className="text-[#4E4483] font-semibold">Request an account</a>.
                </div>
              </div>
            </>
          )}

          <div className="text-[11px] text-[#A69E8B] mt-3.5 text-center">Donor data is confidential. Sign-ins are logged.</div>
        </div>
      </div>
    </div>
  );
};

export default LandingPage;
