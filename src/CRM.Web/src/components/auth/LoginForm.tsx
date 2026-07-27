import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';

const loginSchema = z.object({
  email: z.string().email("Please enter a valid email address."),
  password: z.string().min(6, "Password must be at least 6 characters.")
});

type LoginFormData = z.infer<typeof loginSchema>;

interface LoginFormProps {
  onRealLogin: (email: string, password: string) => Promise<void>;
  onSimulateLogin: (role: string, email: string) => void;
  isDevMode: boolean;
  apiError: string | null;
}

const LoginForm: React.FC<LoginFormProps> = ({
  onRealLogin,
  onSimulateLogin,
  isDevMode,
  apiError
}) => {
  const [activeTab, setActiveTab] = useState<'Donor' | 'Admin' | 'Staff'>('Donor');
  const [isLoading, setIsLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema)
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    try {
      if (isDevMode) {
        // Dev Mode Mock simulation
        await new Promise(resolve => setTimeout(resolve, 500));
        onSimulateLogin(activeTab, data.email);
      } else {
        // Real API Authentication against CRM.API (POST /api/auth/login)
        await onRealLogin(data.email, data.password);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const tabs = ['Donor', 'Admin', 'Staff'] as const;

  return (
    <div className="bg-[#121212]/80 backdrop-blur-xl border border-gray-800 rounded-2xl shadow-2xl p-8 w-full relative overflow-hidden">
      {/* Subtle top border gradient */}
      <div className="absolute top-0 left-0 w-full h-1 bg-gradient-to-r from-transparent via-primary to-transparent opacity-50"></div>
      
      <div className="text-center mb-8">
        <h2 className="text-2xl font-semibold text-white tracking-tight">Sign In</h2>
        <p className="text-gray-400 text-sm mt-2">
          {isDevMode ? 'Dev Mode (Mock Auth & JWT Inspector)' : 'Access your CRM portal via REST API'}
        </p>
      </div>

      {/* Custom Tabs */}
      <div className="flex bg-[#1E1E1E] rounded-lg p-1 mb-6">
        {tabs.map((tab) => (
          <button
            key={tab}
            type="button"
            onClick={() => setActiveTab(tab)}
            className={`flex-1 py-2 px-4 text-sm font-medium rounded-md transition-all duration-200 ${
              activeTab === tab 
                ? 'bg-[#2A2A2A] text-white shadow-sm ring-1 ring-gray-700/50' 
                : 'text-gray-400 hover:text-gray-200'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {/* API Error Alert */}
      {apiError && !isDevMode && (
        <div className="mb-6 p-4 bg-red-950/50 border border-red-800/80 rounded-xl text-red-300 text-sm flex items-start gap-3">
          <svg className="w-5 h-5 text-red-400 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <div>
            <span className="font-semibold block text-red-200">Authentication Failed</span>
            {apiError}
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        <div className="space-y-2">
          <label className="text-sm font-medium text-gray-300">Email Address</label>
          <input
            {...register('email')}
            type="email"
            className="w-full bg-[#1A1A1A] border border-gray-800 rounded-lg px-4 py-3 text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
            placeholder="you@example.com"
          />
          {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
        </div>

        <div className="space-y-2">
          <div className="flex justify-between items-center">
            <label className="text-sm font-medium text-gray-300">Password</label>
            <a href="#" className="text-xs text-primary hover:text-yellow-400 transition-colors">Forgot password?</a>
          </div>
          <input
            {...register('password')}
            type="password"
            className="w-full bg-[#1A1A1A] border border-gray-800 rounded-lg px-4 py-3 text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
            placeholder="••••••••"
          />
          {errors.password && <p className="text-red-400 text-xs mt-1">{errors.password.message}</p>}
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className="w-full bg-primary hover:bg-yellow-400 text-[#0A0A0A] font-semibold rounded-lg px-4 py-3 mt-4 transition-all flex items-center justify-center gap-2 disabled:opacity-70 disabled:cursor-not-allowed"
        >
          {isLoading ? (
            <svg className="animate-spin h-5 w-5 text-[#0A0A0A]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          ) : (
             isDevMode ? 'Simulate Mock Login' : 'Sign In via CRM API'
          )}
        </button>
      </form>
    </div>
  );
};

export default LoginForm;
