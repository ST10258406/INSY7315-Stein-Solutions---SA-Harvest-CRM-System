import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, afterEach, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import LandingPage from './LandingPage';
import { useAuthStore } from '@/store/authStore';

describe('LandingPage', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isHydrating: false,
      isDevBypass: false,
    });
  });
  afterEach(cleanup);

  it('renders the wordmark, mission headline, and both entry actions', () => {
    const { container } = render(
      <MemoryRouter>
        <LandingPage />
      </MemoryRouter>
    );

    expect(screen.getByText('SA Harvest')).toBeInTheDocument();
    expect(container.querySelector('h1')?.textContent).toMatch(/The CRM behind.*South Africa's food rescue/);
    expect(screen.getByRole('button', { name: 'To CRM' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'To SignIn' })).toBeInTheDocument();
  });

  it('degrades gracefully without WebGL — jsdom has none, so this exercises the real fallback path', () => {
    // No mocking involved: jsdom's canvas.getContext('webgl'/'webgl2') returns
    // null, so Swoosh's capability probe genuinely fails here, exactly like a
    // real browser without WebGL would. The page must still be fully usable.
    // The fallback SVG sits under an aria-hidden decorative wrapper (correct
    // — it's purely visual), so this checks DOM presence, not accessible role.
    const { container } = render(
      <MemoryRouter>
        <LandingPage />
      </MemoryRouter>
    );

    expect(container.querySelector('svg[aria-label="Decorative curved swoosh"]')).toBeInTheDocument();
    expect(screen.getByText('SA Harvest')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'To CRM' })).toBeInTheDocument();
  });

  it('shows the minimal footer', () => {
    render(
      <MemoryRouter>
        <LandingPage />
      </MemoryRouter>
    );

    expect(screen.getByText(/© \d{4} SA Harvest NPC/)).toBeInTheDocument();
  });
});
