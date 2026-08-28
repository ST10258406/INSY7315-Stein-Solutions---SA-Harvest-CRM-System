import { Component, type ReactNode } from 'react';

interface Props {
  fallback: ReactNode;
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

/**
 * The upfront WebGL capability probe (useWebglSupport) catches the common
 * case, but context creation can still fail after that check passes (driver
 * quirks, exhausted context limits) — R3F's own recommendation is to wrap
 * `<Canvas>` in an error boundary as the second line of defence.
 */
export class SwooshErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error: unknown) {
    console.error('Swoosh 3D scene failed to render; falling back to static visual.', error);
  }

  render() {
    return this.state.hasError ? this.props.fallback : this.props.children;
  }
}
