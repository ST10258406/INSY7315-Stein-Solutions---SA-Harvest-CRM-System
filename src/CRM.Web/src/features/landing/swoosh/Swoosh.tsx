import { lazy, Suspense, useEffect, useState } from 'react';
import { useReducedMotion } from '@/lib/useReducedMotion';
import { useWebglSupport } from './useWebglSupport';
import { SwooshErrorBoundary } from './SwooshErrorBoundary';
import { SwooshFallback } from './SwooshFallback';

// Code-split: three.js + R3F load in their own chunk, after first paint, so
// the white page/header/text/buttons don't wait on the 3D scene to render.
const SwooshScene = lazy(() => import('./SwooshScene').then((m) => ({ default: m.SwooshScene })));

/**
 * Public entry point for the 3D swoosh. Isolated from the rest of the
 * landing page so it (and its WebGL fallback / error boundary / reduced-
 * motion handling) can be reused anywhere else in the app later.
 */
export function Swoosh() {
  const reducedMotion = useReducedMotion();
  const webglSupported = useWebglSupport();
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    if (!webglSupported) return;
    // Let the first paint (background/header/text) land before mounting the
    // R3F canvas, matching the brief's "white page, then swoosh" sequence.
    const timeout = setTimeout(() => setIsReady(true), 60);
    return () => clearTimeout(timeout);
  }, [webglSupported]);

  if (!webglSupported) {
    return (
      <div className="h-full w-full opacity-70">
        <SwooshFallback />
      </div>
    );
  }

  return (
    <div className="h-full w-full transition-opacity duration-700 ease-out" style={{ opacity: isReady ? 1 : 0 }}>
      <SwooshErrorBoundary fallback={<SwooshFallback />}>
        <Suspense fallback={null}>
          <SwooshScene reducedMotion={reducedMotion} />
        </Suspense>
      </SwooshErrorBoundary>
    </div>
  );
}
