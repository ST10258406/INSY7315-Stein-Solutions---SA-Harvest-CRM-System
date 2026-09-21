import { useEffect, useRef, useState } from 'react';
import SignatureCanvas from 'react-signature-canvas';
import { Eraser } from 'lucide-react';
import { cn } from '@/lib/utils';

interface SignaturePadProps {
  /** Fires with a `data:image/png;base64,...` string once a real signature is drawn, or `null` once cleared/emptied. */
  onChange: (dataUrl: string | null) => void;
  error?: string;
}

const CANVAS_HEIGHT = 200;

// A signature is a handful of long strokes — an accidental single tap/dot
// still registers with signature_pad but produces a point cluster with a
// near-zero bounding box, so a tiny diagonal threshold is enough to tell the
// two apart without needing anything more sophisticated.
const MIN_STROKE_DIAGONAL_PX = 12;

function strokeDiagonal(pointGroups: { x: number; y: number }[][]): number {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;

  for (const group of pointGroups) {
    for (const point of group) {
      if (point.x < minX) minX = point.x;
      if (point.x > maxX) maxX = point.x;
      if (point.y < minY) minY = point.y;
      if (point.y > maxY) maxY = point.y;
    }
  }

  if (minX === Infinity) return 0;
  return Math.hypot(maxX - minX, maxY - minY);
}

export function SignaturePad({ onChange, error }: SignaturePadProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const padRef = useRef<SignatureCanvas>(null);
  const [width, setWidth] = useState(0);
  const [tooSmallHint, setTooSmallHint] = useState(false);

  // The canvas element's own width/height attributes (not just its CSS size)
  // set the drawing surface's resolution, so this measures the actual
  // rendered container width rather than relying on a fixed/hardcoded value
  // that would either overflow on phones or leave desktop looking cramped.
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const observer = new ResizeObserver((entries) => {
      const nextWidth = Math.round(entries[0].contentRect.width);
      setWidth((current) => (Math.abs(current - nextWidth) > 1 ? nextWidth : current));
    });
    observer.observe(container);
    return () => observer.disconnect();
  }, []);

  function handleEnd() {
    const pad = padRef.current;
    if (!pad || pad.isEmpty()) {
      onChange(null);
      return;
    }

    if (strokeDiagonal(pad.toData()) < MIN_STROKE_DIAGONAL_PX) {
      setTooSmallHint(true);
      pad.clear();
      onChange(null);
      return;
    }

    setTooSmallHint(false);
    onChange(pad.toDataURL('image/png'));
  }

  function handleClear() {
    padRef.current?.clear();
    setTooSmallHint(false);
    onChange(null);
  }

  return (
    <div className="flex flex-col gap-3">
      <div ref={containerRef} className="w-full">
        {width > 0 && (
          <div className="relative">
            <SignatureCanvas
              ref={padRef}
              penColor="#16160F"
              backgroundColor="#FFFFFF"
              onEnd={handleEnd}
              clearOnResize={false}
              canvasProps={{
                width,
                height: CANVAS_HEIGHT,
                className: cn(
                  'block w-full cursor-crosshair rounded-[14px] border-2 bg-white',
                  error ? 'border-[#D4373A]' : 'border-[#E4E4DE]'
                ),
                style: { touchAction: 'none', height: CANVAS_HEIGHT },
                'aria-label': 'Signature — draw with your mouse, stylus, or finger',
              }}
            />
            <span className="pointer-events-none absolute bottom-5 left-4 text-xs font-medium text-[#B4B4AA]">Sign here</span>
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="button"
          onClick={handleClear}
          className="flex h-[38px] cursor-pointer items-center gap-1.75 rounded-full border-[1.5px] border-[#E4E4DE] bg-white px-4 text-[12.5px] font-semibold text-[#16160F]"
        >
          <Eraser className="h-3.5 w-3.5" />
          Clear
        </button>
        <span className="text-[11.5px] font-medium text-[#82827A]">Draw your signature above using your mouse or finger.</span>
      </div>

      {tooSmallHint && (
        <span className="text-[11.5px] font-semibold text-[#D4373A]">
          That doesn't look like a signature — please draw it again.
        </span>
      )}
      {error && !tooSmallHint && <span className="text-[11.5px] font-semibold text-[#D4373A]">{error}</span>}
    </div>
  );
}
