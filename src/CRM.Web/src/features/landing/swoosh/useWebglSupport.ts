import { useState } from 'react';

function detectWebglSupport(): boolean {
  if (typeof document === 'undefined') return false;
  try {
    const canvas = document.createElement('canvas');
    return !!(canvas.getContext('webgl2') || canvas.getContext('webgl'));
  } catch {
    return false;
  }
}

/** Cheap synchronous WebGL capability probe, checked once before ever mounting the R3F Canvas. */
export function useWebglSupport(): boolean {
  const [supported] = useState(detectWebglSupport);
  return supported;
}
