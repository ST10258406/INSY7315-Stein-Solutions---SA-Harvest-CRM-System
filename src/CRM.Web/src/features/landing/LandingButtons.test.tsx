import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { LandingButtons } from './LandingButtons';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';

function renderButtons() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route path="/" element={<LandingButtons />} />
        <Route path={paths.dashboard} element={<div data-testid="dashboard">Dashboard</div>} />
        <Route path={paths.login} element={<div data-testid="login">Login</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('LandingButtons', () => {
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
  afterEach(() => {
    cleanup();
    vi.unstubAllEnvs();
  });

  it('renders exactly the two primary actions', () => {
    renderButtons();

    expect(screen.getByRole('button', { name: 'To CRM' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'To SignIn' })).toBeInTheDocument();
  });

  it('"To SignIn" is a plain link to the existing login route', () => {
    renderButtons();

    expect(screen.getByRole('link', { name: 'To SignIn' })).toHaveAttribute('href', paths.login);
  });

  it('in a dev build, "To CRM" enables the auth bypass and navigates straight to the dashboard', async () => {
    const user = userEvent.setup();
    renderButtons(); // import.meta.env.DEV is true by default under vitest

    await user.click(screen.getByRole('button', { name: 'To CRM' }));

    expect(await screen.findByTestId('dashboard')).toBeInTheDocument();
    expect(useAuthStore.getState().isDevBypass).toBe(true);
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });

  it('in a production build, "To CRM" navigates to the dashboard WITHOUT bypassing auth', async () => {
    vi.stubEnv('DEV', false);
    const user = userEvent.setup();
    renderButtons();

    await user.click(screen.getByRole('button', { name: 'To CRM' }));

    expect(await screen.findByTestId('dashboard')).toBeInTheDocument();
    expect(useAuthStore.getState().isDevBypass).toBe(false);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});
