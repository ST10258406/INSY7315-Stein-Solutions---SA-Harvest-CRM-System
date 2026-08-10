import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { resetPasswordSchema, type ResetPasswordFormValues } from '@/features/auth/schemas/resetPasswordSchema';

interface ResetPasswordFormProps {
  onSubmitPasswords: (values: ResetPasswordFormValues) => Promise<void>;
  apiError: string | null;
}

const ResetPasswordForm: React.FC<ResetPasswordFormProps> = ({ onSubmitPasswords, apiError }) => {
  const [showPw, setShowPw] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
  });

  const onSubmit = async (data: ResetPasswordFormValues) => {
    setIsLoading(true);
    try {
      await onSubmitPasswords(data);
    } catch {
      // apiError is surfaced by the parent via props
    } finally {
      setIsLoading(false);
    }
  };

  const hasError = Boolean(apiError);

  return (
    <div className="bg-white border border-[#E4DECE] rounded-xl p-[22px]">
      {hasError && (
        <div className="flex items-start gap-[9px] bg-[#FBE4DC] border border-[#F1CDC0] rounded-lg px-3 py-2.5 mb-4">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#A63E27" strokeWidth={2.2} className="shrink-0 mt-px">
            <circle cx="12" cy="12" r="9" />
            <path d="M12 7.5v5.5M12 16.2v.6" />
          </svg>
          <div className="text-xs text-[#8C3A24] leading-relaxed">{apiError}</div>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-3.5">
        <div>
          <div className="text-[11.5px] font-semibold text-[#3F3A2E] mb-1.5">New password</div>
          <div className={`flex items-center border rounded-lg bg-[#FFFDF8] pr-2.5 ${
            hasError || errors.newPassword ? 'border-[#E8B7A6]' : 'border-[#E4DECE]'
          }`}>
            <input
              {...register('newPassword')}
              type={showPw ? 'text' : 'password'}
              placeholder="••••••••"
              className="flex-1 min-w-0 border-none bg-transparent rounded-lg px-[11px] py-2.5 text-[13px] text-[#17140F] outline-none placeholder:text-[#A69E8B]"
            />
            <button
              type="button"
              onClick={() => setShowPw((v) => !v)}
              className="text-[11px] font-bold text-[#8A5A00] px-1.5 py-1 whitespace-nowrap"
            >
              {showPw ? 'Hide' : 'Show'}
            </button>
          </div>
          {errors.newPassword && <p className="text-[#A63E27] text-xs mt-1">{errors.newPassword.message}</p>}
        </div>

        <div>
          <div className="text-[11.5px] font-semibold text-[#3F3A2E] mb-1.5">Confirm password</div>
          <input
            {...register('confirmPassword')}
            type={showPw ? 'text' : 'password'}
            placeholder="••••••••"
            className={`w-full box-border border rounded-lg px-[11px] py-2.5 text-[13px] text-[#17140F] outline-none bg-[#FFFDF8] placeholder:text-[#A69E8B] ${
              hasError || errors.confirmPassword ? 'border-[#E8B7A6]' : 'border-[#E4DECE]'
            }`}
          />
          {errors.confirmPassword && <p className="text-[#A63E27] text-xs mt-1">{errors.confirmPassword.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className={`w-full text-[#17140F] rounded-lg py-2.5 text-[13.5px] font-bold mt-0.5 transition-colors disabled:cursor-not-allowed ${
            isLoading ? 'bg-[#E4C86A]' : 'bg-[#F2B705] hover:bg-[#E0AC00]'
          }`}
        >
          {isLoading ? 'Resetting…' : 'Reset password'}
        </button>
      </form>
    </div>
  );
};

export default ResetPasswordForm;
