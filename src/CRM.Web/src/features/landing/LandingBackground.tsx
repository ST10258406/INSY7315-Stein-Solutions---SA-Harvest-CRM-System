/**
 * Atmospheric backdrop, original and abstract (no photograph, nothing
 * borrowed): a cool dark "sky" gradient at top settling into the app
 * shell's near-black (#0C0C0A), with a warm brand-yellow glow bleeding out
 * from behind a soft dark ridge silhouette in the lower right — built
 * entirely from CSS gradients, blur, and one blurred SVG shape.
 */
export function LandingBackground() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 overflow-hidden bg-[#0C0C0A]">
      <div className="absolute inset-0 bg-gradient-to-b from-[#0B1416] via-[#0C0C0A] to-[#0C0C0A]" />

      <div
        className="absolute -right-[5%] -bottom-[20%] h-[85%] w-[65%] rounded-full opacity-[0.32] blur-[110px]"
        style={{ background: 'radial-gradient(circle, rgb(250 223 1) 0%, rgba(250,223,1,0.35) 45%, transparent 72%)' }}
      />
      <div
        className="absolute right-[6%] bottom-[8%] h-[42%] w-[42%] rounded-full opacity-[0.22] blur-[90px]"
        style={{ background: 'radial-gradient(circle, #E8794A 0%, transparent 70%)' }}
      />

      <svg
        viewBox="0 0 1440 900"
        preserveAspectRatio="none"
        className="absolute inset-0 h-full w-full opacity-90 blur-[6px]"
      >
        <path
          d="M 1440 900 L 1440 430 C 1260 470, 1140 560, 980 640 C 830 715, 700 760, 560 900 Z"
          fill="#0C0C0A"
        />
      </svg>

      <div className="absolute inset-0 bg-gradient-to-r from-[#0C0C0A] via-[#0C0C0A]/40 to-transparent" />
      <div className="absolute inset-0 bg-gradient-to-b from-[#0C0C0A] via-transparent to-[#0C0C0A]/70" />
    </div>
  );
}
