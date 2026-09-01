import { Link, useNavigate } from 'react-router-dom';
import { paths } from '@/routes/paths';
import { useAuthStore } from '@/store/authStore';

const buttonClasses =
  'inline-flex h-11 w-full items-center justify-center gap-2 rounded-lg border border-white/15 bg-white/[0.03] px-7 text-[13.5px] font-semibold tracking-tight text-[#F4F4EE] backdrop-blur-sm transition-all duration-200 ease-out hover:-translate-y-px hover:border-white/30 hover:bg-white/[0.08] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--brand)] active:translate-y-0 sm:w-auto sm:min-w-[168px]';

/**
 * The two — and only two — primary actions into the system. "To CRM" bypasses
 * auth in dev builds only (see authStore.enableDevBypass). In production it
 * just navigates to the dashboard route and lets ProtectedRoute enforce auth
 * (redirecting unauthenticated visitors back here) — the button itself must
 * not duplicate that check.
 * "To SignIn" always goes through the existing auth flow — there's no
 * separate "Primer Auth" in this codebase, so this points at the real one.
 */
export function LandingButtons() {
  const navigate = useNavigate();
  const enableDevBypass = useAuthStore((s) => s.enableDevBypass);

  function handleToCrm() {
    if (import.meta.env.DEV) {
      enableDevBypass();
    }

    navigate(paths.dashboard);
  }

  return (
    <div className="flex w-full max-w-xs flex-col items-stretch gap-3 sm:w-auto sm:max-w-none sm:flex-row sm:items-center sm:gap-4">
      <button type="button" onClick={handleToCrm} className={buttonClasses}>
        To CRM
      </button>
      <Link to={paths.login} className={buttonClasses}>
        To SignIn
      </Link>
    </div>
  );
}
