import { LandingBackground } from './LandingBackground';
import { LandingLineArt } from './LandingLineArt';
import { LandingHero } from './LandingHero';
import { LandingFooter } from './LandingFooter';

/**
 * The public entry point (/) — a dark, atmospheric hero sharing the existing
 * dark app-shell palette (#0C0C0A / #F4F4EE / --brand) rather than a separate
 * light theme, so the landing page and the CRM it leads into read as one
 * product. See features/shell/AppLayout.tsx for the same base colours.
 */
export default function LandingPage() {
  return (
    <div
      className="relative flex min-h-screen flex-col overflow-hidden bg-[#0C0C0A] text-[#F4F4EE]"
      style={{ fontFamily: "'IBM Plex Sans', system-ui, sans-serif" }}
    >
      <LandingBackground />
      <LandingLineArt />
      <LandingHero />
      <LandingFooter />
    </div>
  );
}
