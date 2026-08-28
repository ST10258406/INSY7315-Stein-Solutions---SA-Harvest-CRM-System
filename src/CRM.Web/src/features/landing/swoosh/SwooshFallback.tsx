/**
 * Static stand-in shown when WebGL is unavailable or the 3D scene errors.
 * Traces roughly the same hook shape as the 3D ribbon so the composition
 * still reads correctly — the page must remain intelligible without WebGL.
 */
export function SwooshFallback() {
  return (
    <svg
      viewBox="0 0 400 300"
      role="img"
      aria-label="Decorative curved swoosh"
      className="h-full w-full"
    >
      <defs>
        <linearGradient id="swoosh-fallback-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="var(--brand)" stopOpacity="0.9" />
          <stop offset="100%" stopColor="var(--brand)" stopOpacity="0.35" />
        </linearGradient>
      </defs>
      <path
        d="M 370 60 C 300 20, 200 40, 160 110 C 120 180, 60 190, 40 230"
        fill="none"
        stroke="url(#swoosh-fallback-gradient)"
        strokeWidth="26"
        strokeLinecap="round"
      />
    </svg>
  );
}
