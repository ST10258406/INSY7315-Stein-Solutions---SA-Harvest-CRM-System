// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, cleanup, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import userEvent from '@testing-library/user-event';
import { forwardRef, useImperativeHandle } from 'react';
import { SignaturePad } from './SignaturePad';

// jsdom has no real <canvas> 2D context, so react-signature-canvas can't
// actually draw in tests — this stands in for it with a plain element whose
// imperative handle (isEmpty/clear/toData/toDataURL) the test controls
// directly, and exposes onEnd as a clickable trigger to simulate "finished a
// stroke" without needing real pointer-drag canvas events.
let mockIsEmpty = true;
let mockPoints: { x: number; y: number }[][] = [];

vi.mock('react-signature-canvas', () => ({
  default: forwardRef((props: { onEnd?: () => void }, ref) => {
    useImperativeHandle(ref, () => ({
      isEmpty: () => mockIsEmpty,
      clear: () => {
        mockIsEmpty = true;
      },
      toData: () => mockPoints,
      toDataURL: () => 'data:image/png;base64,MOCK',
    }));
    return <button type="button" data-testid="mock-canvas" onClick={() => props.onEnd?.()} />;
  }),
}));

// ResizeObserver isn't implemented in jsdom.
class MockResizeObserver {
  callback: ResizeObserverCallback;
  constructor(callback: ResizeObserverCallback) {
    this.callback = callback;
  }
  observe(target: Element) {
    this.callback([{ contentRect: { width: 740 } } as ResizeObserverEntry], this as unknown as ResizeObserver);
    void target;
  }
  unobserve() {}
  disconnect() {}
}
vi.stubGlobal('ResizeObserver', MockResizeObserver);

describe('SignaturePad', () => {
  afterEach(() => {
    cleanup();
    mockIsEmpty = true;
    mockPoints = [];
  });

  it('reports null while the canvas is empty', () => {
    const onChange = vi.fn();
    render(<SignaturePad onChange={onChange} />);

    mockIsEmpty = true;
    fireEvent.click(screen.getByTestId('mock-canvas'));

    expect(onChange).toHaveBeenCalledWith(null);
  });

  it('reports a base64 PNG once a real stroke is drawn', () => {
    const onChange = vi.fn();
    render(<SignaturePad onChange={onChange} />);

    mockIsEmpty = false;
    mockPoints = [
      [
        { x: 10, y: 10 },
        { x: 50, y: 40 },
      ],
    ];
    fireEvent.click(screen.getByTestId('mock-canvas'));

    expect(onChange).toHaveBeenCalledWith('data:image/png;base64,MOCK');
  });

  it('rejects a single accidental tap/dot as too small to be a signature', () => {
    const onChange = vi.fn();
    render(<SignaturePad onChange={onChange} />);

    mockIsEmpty = false;
    mockPoints = [[{ x: 10, y: 10 }, { x: 11, y: 10 }]]; // 1px diagonal — well under the threshold
    fireEvent.click(screen.getByTestId('mock-canvas'));

    expect(onChange).toHaveBeenCalledWith(null);
    expect(screen.getByText(/doesn't look like a signature/i)).toBeInTheDocument();
  });

  it('clears via the Clear button and reports null', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<SignaturePad onChange={onChange} />);

    mockIsEmpty = false;
    mockPoints = [[{ x: 10, y: 10 }, { x: 50, y: 40 }]];
    fireEvent.click(screen.getByTestId('mock-canvas'));
    onChange.mockClear();

    await user.click(screen.getByRole('button', { name: /clear/i }));

    expect(onChange).toHaveBeenCalledWith(null);
  });

  it('shows the passed-in error message', () => {
    render(<SignaturePad onChange={vi.fn()} error="A signature is required before you can continue." />);

    expect(screen.getByText(/a signature is required/i)).toBeInTheDocument();
  });
});
