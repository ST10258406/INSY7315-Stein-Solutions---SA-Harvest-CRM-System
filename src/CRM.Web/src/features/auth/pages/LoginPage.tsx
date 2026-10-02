import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LoginForm from '@/components/auth/LoginForm';
import AuthBrandPanel from '@/components/auth/AuthBrandPanel';
import { useLogin } from '@/features/auth/hooks/useLogin';
import { paths } from '@/routes/paths';

const LoginPage: React.FC = () => {
  const [apiError, setApiError] = useState<string | null>(null);
  const { mutateAsync: loginMutation } = useLogin();
  const navigate = useNavigate();

  const handleLogin = async (email: string, password: string) => {
    setApiError(null);
    try {
      await loginMutation({ email, password });
      navigate(paths.dashboard);
    } catch {
      setApiError('Login failed. Please check your credentials or API connection.');
    }
  };

  return (
    <div
      className="flex h-screen w-full overflow-hidden bg-[#F4F2EC] text-[#17140F]"
    >
      <AuthBrandPanel />

      {/* Form panel */}
      <div className="flex-1 flex items-center justify-center p-8 overflow-y-auto">
        <div className="w-full max-w-[396px]">
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

          <div className="text-[11px] text-[#A69E8B] mt-3.5 text-center">Donor data is confidential. Sign-ins are logged.</div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
