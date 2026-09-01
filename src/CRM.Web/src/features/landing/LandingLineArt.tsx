/**
 * Thin architectural line art — a large arc and two diagonals, almost
 * invisible at a glance. Purely decorative geometry, no grid: it should read
 * as structure, not graph paper.
 */
export function LandingLineArt() {
  return (
    <svg
      aria-hidden
      viewBox="0 0 1440 900"
      preserveAspectRatio="none"
      className="pointer-events-none absolute inset-0 h-full w-full"
    >
      <circle
        cx="420"
        cy="900"
        r="820"
        fill="none"
        stroke="#F4F4EE"
        strokeOpacity="0.14"
        strokeWidth="1"
      />
      <line x1="1160" y1="0" x2="1440" y2="330" stroke="#F4F4EE" strokeOpacity="0.14" strokeWidth="1" />
      <line
        x1="960"
        y1="60"
        x2="1440"
        y2="560"
        stroke="#F4F4EE"
        strokeOpacity="0.1"
        strokeWidth="1"
        strokeDasharray="4 6"
      />
    </svg>
  );
}
