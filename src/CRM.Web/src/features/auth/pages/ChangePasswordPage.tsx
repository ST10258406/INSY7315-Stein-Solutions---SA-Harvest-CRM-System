import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Navigate, useNavigate } from 'react-router-dom';
import AuthBrandPanel from '@/components/auth/AuthBrandPanel';
import { useChangePassword } from '@/features/auth/hooks/useChangePassword';
import { useLogout } from '@/features/auth/hooks/useLogout';
import { changePasswordSchema, type ChangePasswordFormValues } from '@/features/auth/schemas/changePasswordSchema';
import { getFieldError } from '@/lib/apiError';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';

const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';
const WRONG_CURRENT_MESSAGE = 'Your current password is incorrect.';

const inputClass =
  'w-full box-border border rounded-lg px-[11px] py-2.5 text-[13px] text-[#17140F] outline-none bg-[#FFFDF8] placeholder:text-[#A69E8B]';

// Shown to a user whose account still has a seeded/temporary password. The API refuses every
// other request until this succeeds (PasswordChangeRequiredMiddleware); this screen is the UX.
const ChangePasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const mustChange = useAuthStore((s) => s.user?.mustChangePassword === true);
  const [apiError, setApiError] = useState<string | null>(null);
  const [showPw, setShowPw] = useState(false);
  const { mutateAsync: changePassword, isPending } = useChangePassword();
  const { mutate: logout } = useLogout();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({ resolver: zodResolver(changePasswordSchema) });

  // Nothing to do here unless the server said so (also covers the moment after success).
  if (!mustChange) {
    return <Navigate to={paths.dashboard} replace />;
  }

  const onSubmit = async (values: ChangePasswordFormValues) => {
    setApiError(null);
    try {
      await changePassword(values);
      navigate(paths.dashboard, { replace: true });
    } catch (err: unknown) {
      setApiError(
        getFieldError(err, 'CurrentPassword')
          ? WRONG_CURRENT_MESSAGE
          : (getFieldError(err, 'NewPassword') ?? GENERIC_ERROR_MESSAGE),
      );
    }
  };

  const borderFor = (hasError: boolean) => (hasError ? 'border-[#E8B7A6]' : 'border-[#E4DECE]');

  return (
    <div
      className="flex h-screen w-full overflow-hidden bg-[#F4F2EC] text-[#17140F]"
      style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}
    >
      <AuthBrandPanel />

      <div className="flex-1 flex items-center justify-center p-8 overflow-y-auto">
        <div className="w-full max-w-[396px]">
          <div className="text-[23px] font-bold mb-1">Choose a new password</div>
          <div className="text-[13px] text-[#8A8374] mb-[22px]">
            You are using a temporary password. Set your own password to continue.
          </div>

          <div className="bg-white border border-[#E4DECE] rounded-xl p-[22px]">
            {apiError && (
              <div className="bg-[#FBE4DC] border border-[#F1CDC0] rounded-lg px-3 py-2.5 mb-4 text-xs text-[#8C3A24] leading-relaxed">
                {apiError}
              </div>
            )}

            <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-3.5">
              <div>
                <div className="text-[11.5px] font-semibold text-[#3F3A2E] mb-1.5">Current (temporary) password</div>
                <input
                  {...register('currentPassword')}
                  type={showPw ? 'text' : 'password'}
                  autoComplete="current-password"
                  className={`${inputClass} ${borderFor(Boolean(errors.currentPassword))}`}
                />
                {errors.currentPassword && <p className="text-[#A63E27] text-xs mt-1">{errors.currentPassword.message}</p>}
              </div>

              <div>
                <div className="text-[11.5px] font-semibold text-[#3F3A2E] mb-1.5">New password</div>
                <input
                  {...register('newPassword')}
                  type={showPw ? 'text' : 'password'}
                  autoComplete="new-password"
                  className={`${inputClass} ${borderFor(Boolean(errors.newPassword))}`}
                />
                {errors.newPassword && <p className="text-[#A63E27] text-xs mt-1">{errors.newPassword.message}</p>}
              </div>

              <div>
                <div className="text-[11.5px] font-semibold text-[#3F3A2E] mb-1.5">Confirm new password</div>
                <input
                  {...register('confirmPassword')}
                  type={showPw ? 'text' : 'password'}
                  autoComplete="new-password"
                  className={`${inputClass} ${borderFor(Boolean(errors.confirmPassword))}`}
                />
                {errors.confirmPassword && <p className="text-[#A63E27] text-xs mt-1">{errors.confirmPassword.message}</p>}
              </div>

              <button
                type="button"
                onClick={() => setShowPw((v) => !v)}
                className="self-start text-[11px] font-bold text-[#8A5A00]"
              >
                {showPw ? 'Hide passwords' : 'Show passwords'}
              </button>

              <button
                type="submit"
                disabled={isPending}
                className={`w-full text-[#17140F] rounded-lg py-2.5 text-[13.5px] font-bold transition-colors disabled:cursor-not-allowed ${
                  isPending ? 'bg-[#E4C86A]' : 'bg-[#F2B705] hover:bg-[#E0AC00]'
                }`}
              >
                {isPending ? 'Saving…' : 'Change password'}
              </button>
            </form>
          </div>

          <button
            type="button"
            onClick={() => logout()}
            className="mt-4 text-[12px] font-semibold text-[#8A5A00] hover:text-[#B65C36]"
          >
            Sign out
          </button>
        </div>
      </div>
    </div>
  );
};

export default ChangePasswordPage;
