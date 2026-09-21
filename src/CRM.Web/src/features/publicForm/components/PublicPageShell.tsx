import { useEffect, type CSSProperties, type ReactNode } from 'react';
import logoImg from '@/assets/sa-harvest-logo.png';

// The app shell defaults every visitor with no stored preference to dark
// mode (see themeStore.ts), which flips the shared `--card`/`--ink`/
// `--border`/`--hover`/`--muted-*` tokens that Button and friends read. A
// public donor has no reason to see that toggle at all, so this scope pins
// those tokens to the approved design's fixed light palette — CSS custom
// property inheritance means every descendant (including shared components)
// resolves to these values regardless of the ambient `.dark` class higher
// up the tree.
const LIGHT_SCOPE_VARS = {
  '--page': '#FFFFFF',
  '--card': '#FFFFFF',
  '--soft': '#F6F6F3',
  '--border': '#E4E4DE',
  '--hair': '#EDEDE8',
  '--ink': '#16160F',
  '--muted-c': '#82827A',
  '--muted2': '#9A9A90',
  '--hover': '#EFEFEB',
  '--shadow': 'rgba(20, 20, 15, .05)',
  '--brand': '#FADF01',
  '--primary-foreground': '#16160F',
  '--destructive': '#D4373A',
  '--muted-foreground': '#82827A',
} as CSSProperties;

interface PublicPageShellProps {
  children: ReactNode;
}

/**
 * Shared header/footer/background chrome for every screen of the public
 * donor flow — the multi-step form (PublicFormLayout) and whatever comes
 * after it (document upload, confirmation) both sit inside this, so a donor
 * never sees a jarring shell change mid-flow even though internally these
 * are different React components/phases.
 */
export function PublicPageShell({ children }: PublicPageShellProps) {
  // The app's <body> is dark by default (base layer applies bg-background,
  // which defaults dark — see themeStore.ts) and that's outside this
  // component's own DOM subtree, so the CSS-variable scoping above can't
  // reach it. Without this, elastic/overscroll bounce at the top or bottom
  // of the page flashes the dark body background behind this otherwise
  // fully-light public page.
  useEffect(() => {
    const previousBackground = document.body.style.backgroundColor;
    document.body.style.backgroundColor = '#FFFFFF';
    return () => {
      document.body.style.backgroundColor = previousBackground;
    };
  }, []);

  return (
    <div
      className="flex min-h-screen flex-col bg-white text-[#16160F]"
      style={{ fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif", ...LIGHT_SCOPE_VARS }}
    >
      <header className="flex flex-col items-center gap-3.5 px-6 pt-11 pb-2.5 text-center">
        <img src={logoImg} alt="" className="h-[78px] w-[78px] rounded-[18px] object-cover" />
        <div>
          <div className="text-2xl font-extrabold tracking-tight">S.A. Harvest</div>
          <div className="mt-0.5 text-[10.5px] font-bold tracking-[2px] text-[var(--muted-c)]">DONOR ONBOARDING</div>
        </div>
        <p className="mx-auto max-w-[460px] text-[14.5px] leading-relaxed font-medium text-[var(--muted-c)]">
          Partner with us to fight food insecurity in South Africa.
        </p>
      </header>

      <main className="mx-auto w-full max-w-[860px] flex-1 px-6 pt-7 pb-15">{children}</main>

      <footer className="px-6 pt-5.5 pb-8.5 text-center">
        <p className="text-[11.5px] font-medium text-[var(--muted2)]">
          S.A. Harvest NPC · Rescuing food, fighting hunger · Your information is protected under POPIA.
        </p>
      </footer>
    </div>
  );
}
