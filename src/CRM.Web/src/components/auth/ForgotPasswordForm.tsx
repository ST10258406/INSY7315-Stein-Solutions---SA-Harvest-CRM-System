import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { forgotPasswordSchema, type ForgotPasswordFormValues } from '@/features/auth/schemas/forgotPasswordSchema';

interface ForgotPasswordFormProps {
  onSubmitEmail: (email: string) => Promise<void>;
  apiError: string | null;
}

const ForgotPasswordForm: React.FC<ForgotPasswordFormProps> = ({ onSubmitEmail, apiError }) => {
  const [isLoading, setIsLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = async (data: ForgotPasswordFormValues) => {
    setIsLoading(true);
    try {
      await onSubmitEmail(data.email);
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

        <button
          type="submit"
          disabled={isLoading}
          className={`w-full text-[#17140F] rounded-lg py-2.5 text-[13.5px] font-bold mt-0.5 transition-colors disabled:cursor-not-allowed ${
            isLoading ? 'bg-[#E4C86A]' : 'bg-[#F2B705] hover:bg-[#E0AC00]'
          }`}
        >
          {isLoading ? 'Sending…' : 'Send reset link'}
        </button>
      </form>
    </div>
  );
};

export default ForgotPasswordForm;
