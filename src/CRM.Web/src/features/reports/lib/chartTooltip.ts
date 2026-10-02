import type { CSSProperties } from 'react';

/**
 * Theme-aware styling for every Recharts <Tooltip> on the Reports page. Recharts' default is a
 * hard-coded white box whose label takes --ink (near-white in dark mode) and whose value takes the
 * series colour (brand yellow) — unreadable on white in both themes. Spread onto each Tooltip.
 */
export const CHART_TOOLTIP_PROPS = {
  contentStyle: {
    background: 'var(--popover)',
    border: '1px solid var(--border)',
    borderRadius: 12,
    boxShadow: '0 8px 24px var(--shadow)',
    padding: '8px 12px',
  } satisfies CSSProperties,
  labelStyle: { color: 'var(--ink)', fontWeight: 700, marginBottom: 2 } satisfies CSSProperties,
  itemStyle: { color: 'var(--ink)', fontWeight: 600 } satisfies CSSProperties,
};
