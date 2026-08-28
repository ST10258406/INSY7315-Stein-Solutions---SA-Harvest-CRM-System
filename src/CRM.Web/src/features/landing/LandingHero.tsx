import logoImg from '@/assets/sa-harvest-logo.png';
import { Swoosh } from './swoosh/Swoosh';
import { LandingButtons } from './LandingButtons';

/**
 * Left-aligned content column over the atmospheric backdrop, with the 3D
 * swoosh filling the right side of the frame the way the reference's
 * mountain/sky art does. Text always stays on solid dark ground so contrast
 * never depends on where the swoosh happens to be.
 */
export function LandingHero() {
  return (
    <section className="relative z-10 flex flex-1 items-center px-6 sm:px-12 lg:px-20">
      <div
        className="pointer-events-none absolute top-0 right-0 h-[42vh] w-[38%] sm:h-[60vh] sm:w-[48%] lg:h-full lg:w-[55%] motion-safe:animate-in motion-safe:fade-in motion-safe:duration-1000"
        aria-hidden
      >
        <Swoosh />
      </div>

      <div className="relative max-w-xl translate-y-[6vh] lg:max-w-2xl lg:translate-y-[8vh]">
        <div
          className="motion-safe:fill-mode-both flex items-center gap-2.5 motion-safe:animate-in motion-safe:fade-in motion-safe:duration-700"
        >
          <img src={logoImg} alt="" className="h-6 w-6 rounded-md object-cover" />
          <span className="text-xs font-bold tracking-[0.18em] text-[#B9B9AE] uppercase">SA Harvest</span>
        </div>

        <h1
          className="motion-safe:fill-mode-both mt-6 text-[clamp(2.1rem,4.6vw,3.25rem)] leading-[1.1] font-bold tracking-[-0.03em] text-[#F4F4EE] motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-bottom-4 motion-safe:duration-700 motion-safe:delay-200"
        >
          The CRM behind
          <br />
          South Africa&apos;s food rescue
        </h1>

        <p
          className="motion-safe:fill-mode-both mt-5 max-w-md text-[15px] leading-relaxed text-[#8C8578] motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-bottom-2 motion-safe:duration-700 motion-safe:delay-400"
        >
          Donor relationships, compliance, and impact reporting — in one platform built for the teams moving surplus food to where it&apos;s needed.
        </p>

        <div
          className="motion-safe:fill-mode-both mt-9 motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-bottom-2 motion-safe:duration-700 motion-safe:delay-700"
        >
          <LandingButtons />
        </div>
      </div>
    </section>
  );
}
