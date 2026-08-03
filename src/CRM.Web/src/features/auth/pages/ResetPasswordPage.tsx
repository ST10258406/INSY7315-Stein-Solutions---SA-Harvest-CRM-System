import React, { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import ResetPasswordForm from '@/components/auth/ResetPasswordForm';
import AuthBrandPanel from '@/components/auth/AuthBrandPanel';
import { useResetPassword } from '@/features/auth/hooks/useResetPassword';
import { getFieldError } from '@/lib/apiError';
import { paths } from '@/routes/paths';
import type { ResetPasswordFormValues } from '@/features/auth/schemas/resetPasswordSchema';

const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';
const INVALID_LINK_MESSAGE = 'This reset link is invalid or has expired.';

const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const { mutateAsync: resetPasswordMutation } = useResetPassword();

  const token = searchParams.get('token');
  const email = searchParams.get('email');

  if (!token || !email) {
    return (
      <div
        className="flex h-screen w-full overflow-hidden bg-[#F4F2EC] text-[#17140F]"
        style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}
      >
        <AuthBrandPanel />
        <div className="flex-1 flex items-center justify-center p-8 overflow-y-auto">
          <div className="w-full max-w-[396px]">
            <div className="text-[23px] font-bold mb-1">Reset your password</div>
            <div className="bg-white border border-[#E4DECE] rounded-xl p-[22px] text-[13px] text-[#3F3A2E] leading-relaxed">
              {INVALID_LINK_MESSAGE} Request a new one from the{' '}
              <Link to={paths.forgotPassword} className="font-semibold text-[#8A5A00] hover:text-[#B65C36]">
                forgot password
              </Link>{' '}
              page.
            </div>
          </div>
        </div>
      </div>
    );
  }

  const handleSubmitPasswords = async (values: ResetPasswordFormValues) => {
    setApiError(null);
    try {
      await resetPasswordMutation({ ...values, token, email });
      setSubmitted(true);
    } catch (err: unknown) {
      const tokenError = getFieldError(err, 'Token');
      setApiError(tokenError ? INVALID_LINK_MESSAGE : GENERIC_ERROR_MESSAGE);
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
          <div className="text-[23px] font-bold mb-1">Reset your password</div>
          <div className="text-[13px] text-[#8A8374] mb-[22px]">Choose a new password for your account.</div>

          {submitted ? (
            <div className="bg-white border border-[#E4DECE] rounded-xl p-[22px] flex flex-col gap-3.5">
              <div className="text-[13px] text-[#3F3A2E] leading-relaxed">
                Your password has been reset. You can now sign in with your new password.
              </div>
              <button
                type="button"
                onClick={() => navigate(paths.login)}
                className="w-full text-[#17140F] rounded-lg py-2.5 text-[13.5px] font-bold bg-[#F2B705] hover:bg-[#E0AC00] transition-colors"
              >
                Continue to sign in
              </button>
            </div>
          ) : (
            <ResetPasswordForm onSubmitPasswords={handleSubmitPasswords} apiError={apiError} />
          )}
        </div>
      </div>
    </div>
  );
};

export default ResetPasswordPage;
