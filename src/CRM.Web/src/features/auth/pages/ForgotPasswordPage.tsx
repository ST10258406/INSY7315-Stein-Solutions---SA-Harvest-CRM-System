import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import ForgotPasswordForm from '@/components/auth/ForgotPasswordForm';
import AuthBrandPanel from '@/components/auth/AuthBrandPanel';
import { useForgotPassword } from '@/features/auth/hooks/useForgotPassword';
import { paths } from '@/routes/paths';

const GENERIC_SUCCESS_MESSAGE =
  "If an account exists for that email, we've sent a password reset link. Check your inbox.";
const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';

const ForgotPasswordPage: React.FC = () => {
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const { mutateAsync: forgotPasswordMutation } = useForgotPassword();

  const handleSubmitEmail = async (email: string) => {
    setApiError(null);
    try {
      await forgotPasswordMutation({ email });
      setSubmitted(true);
    } catch (err: unknown) {
      setApiError(GENERIC_ERROR_MESSAGE);
      throw err;
    }
  };

  return (
    <div
      className="flex h-screen w-full overflow-hidden bg-[#F4F2EC] text-[#17140F]"
      style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}
    >
      <AuthBrandPanel />

      <div className="flex-1 flex items-center justify-center p-8 overflow-y-auto">
        <div className="w-full max-w-[396px]">
          <div className="text-[23px] font-bold mb-1">Forgot your password?</div>
          <div className="text-[13px] text-[#8A8374] mb-[22px]">
            Enter your work email and we'll send you a reset link.
          </div>

          {submitted ? (
            <div className="bg-white border border-[#E4DECE] rounded-xl p-[22px] text-[13px] text-[#3F3A2E] leading-relaxed">
              {GENERIC_SUCCESS_MESSAGE}
            </div>
          ) : (
            <ForgotPasswordForm onSubmitEmail={handleSubmitEmail} apiError={apiError} />
          )}

          <div className="text-[12px] text-[#8A8374] mt-3.5 text-center">
            <Link to={paths.login} className="font-semibold text-[#8A5A00] hover:text-[#B65C36]">
              Back to sign in
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ForgotPasswordPage;
