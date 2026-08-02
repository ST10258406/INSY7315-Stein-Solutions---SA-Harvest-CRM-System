import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema, type LoginFormValues } from '@/features/auth/schemas/loginSchema';

interface LoginFormProps {
  onLogin: (email: string, password: string) => Promise<void>;
  apiError: string | null;
}

const LoginForm: React.FC<LoginFormProps> = ({ onLogin, apiError }) => {
  const [showPw, setShowPw] = useState(false);
  const [remember, setRemember] = useState(true);
  const [isLoading, setIsLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormValues) => {
    setIsLoading(true);
    try {
      await onLogin(data.email, data.password);
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

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3.5">
        <div>
          <div className="text-[11.5px] font-semibold text-[#3F3A2E] mb-1.5">Work email</div>
          <input
            {...register('email')}
            type="email"
            placeholder="name@saharvest.org"
            className={`w-full box-border border rounded-lg px-[11px] py-2.5 text-[13px] text-[#17140F] outline-none bg-[#FFFDF8] placeholder:text-[#A69E8B] ${
              hasError || errors.email ? 'border-[#E8B7A6]' : 'border-[#E4DECE]'
            }`}
          />
          {errors.email && <p className="text-[#A63E27] text-xs mt-1">{errors.email.message}</p>}
        </div>

        <div>
          <div className="flex items-center justify-between mb-1.5">
            <div className="text-[11.5px] font-semibold text-[#3F3A2E]">Password</div>
            <a href="#" className="text-[11.5px] font-semibold text-[#8A5A00] hover:text-[#B65C36]">Forgot?</a>
          </div>
          <div className={`flex items-center border rounded-lg bg-[#FFFDF8] pr-2.5 ${
            hasError || errors.password ? 'border-[#E8B7A6]' : 'border-[#E4DECE]'
          }`}>
            <input
              {...register('password')}
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
          {errors.password && <p className="text-[#A63E27] text-xs mt-1">{errors.password.message}</p>}
        </div>

        <div className="flex items-center justify-between pt-0.5">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              className="sr-only"
              checked={remember}
              onChange={() => setRemember((v) => !v)}
            />
            <span className={`w-[15px] h-[15px] border-[1.5px] rounded flex items-center justify-center flex-none ${
              remember ? 'bg-[#F2B705] border-[#F2B705]' : 'bg-white border-[#D8D0BC]'
            }`}>
              {remember && (
                <svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="#17140F" strokeWidth={3.4}>
                  <path d="M5 13l4 4L19 7" />
                </svg>
              )}
            </span>
            <span className="text-xs text-[#3F3A2E]">Keep me signed in</span>
          </label>
          <span className="text-[11px] text-[#A69E8B]">7 days</span>
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className={`w-full text-[#17140F] rounded-lg py-2.5 text-[13.5px] font-bold mt-0.5 transition-colors disabled:cursor-not-allowed ${
            isLoading ? 'bg-[#E4C86A]' : 'bg-[#F2B705] hover:bg-[#E0AC00]'
          }`}
        >
          {isLoading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <div className="flex items-center gap-2.5 my-[18px]">
        <div className="flex-1 h-px bg-[#EDE7D9]" />
        <div className="text-[10.5px] font-bold tracking-wide text-[#A69E8B]">OR</div>
        <div className="flex-1 h-px bg-[#EDE7D9]" />
      </div>

      <button
        type="button"
        disabled
        title="Microsoft 365 sign-in is coming soon"
        className="w-full flex items-center justify-center gap-2 bg-white border border-[#DDD6C4] rounded-lg py-2.5 text-xs font-semibold text-[#3F3A2E] opacity-60 cursor-not-allowed"
      >
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#6E6857" strokeWidth={2}>
          <rect x="3" y="3" width="8" height="8" rx="1" />
          <rect x="13" y="3" width="8" height="8" rx="1" />
          <rect x="3" y="13" width="8" height="8" rx="1" />
          <rect x="13" y="13" width="8" height="8" rx="1" />
        </svg>
        Continue with Microsoft 365
      </button>
    </div>
  );
};

export default LoginForm;
