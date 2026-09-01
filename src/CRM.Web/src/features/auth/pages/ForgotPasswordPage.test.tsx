import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import ForgotPasswordPage from './ForgotPasswordPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { api } from '@/lib/axios';

vi.mock('@/lib/axios', () => ({
  api: {
    post: vi.fn(),
  },
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false },
  },
});

const renderPage = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ForgotPasswordPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  afterEach(() => {
    cleanup();
  });

  it('Empty submit → shows required-field error, no API call made', async () => {
    renderPage();

    const submitBtn = screen.getByRole('button', { name: /Send reset link/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Invalid email format → shows email validation error, no API call made', async () => {
    renderPage();

    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    fireEvent.change(emailInput, { target: { value: 'not-an-email' } });

    const submitBtn = screen.getByRole('button', { name: /Send reset link/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Enter a valid email address')).toBeInTheDocument();
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Valid submit → mutation fires with correct payload', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { message: 'If this email address exists, a reset link has been sent.' } });

    renderPage();

    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    fireEvent.change(emailInput, { target: { value: 'test@saharvest.org' } });
    fireEvent.click(screen.getByRole('button', { name: /Send reset link/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/api/auth/forgot-password', { email: 'test@saharvest.org' });
    });
  });

  it('Successful submit → shows generic success message (regardless of whether the account exists)', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { message: 'If this email address exists, a reset link has been sent.' } });

    renderPage();

    fireEvent.change(screen.getByPlaceholderText('name@saharvest.org'), { target: { value: 'unknown@saharvest.org' } });
    fireEvent.click(screen.getByRole('button', { name: /Send reset link/i }));

    await waitFor(() => {
      expect(screen.getByText(/we've sent a password reset link/i)).toBeInTheDocument();
    });
  });

  it('Failed submit (network/server error) → shows generic error message, no success message', async () => {
    vi.mocked(api.post).mockRejectedValueOnce(new Error('Network Error'));

    renderPage();

    fireEvent.change(screen.getByPlaceholderText('name@saharvest.org'), { target: { value: 'test@saharvest.org' } });
    fireEvent.click(screen.getByRole('button', { name: /Send reset link/i }));

    await waitFor(() => {
      expect(screen.getByText('Something went wrong. Please try again.')).toBeInTheDocument();
    });
    expect(screen.queryByText(/we've sent a password reset link/i)).not.toBeInTheDocument();
  });

  it('Submit button disabled and shows loading text while isPending', async () => {
    let resolvePost!: (value?: unknown) => void;
    const postPromise = new Promise((resolve) => {
      resolvePost = resolve;
    });
    vi.mocked(api.post).mockReturnValueOnce(postPromise);

    renderPage();

    fireEvent.change(screen.getByPlaceholderText('name@saharvest.org'), { target: { value: 'test@saharvest.org' } });
    fireEvent.click(screen.getByRole('button', { name: /Send reset link/i }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Sending…/i })).toBeDisabled();
    });

    resolvePost({ data: { message: 'ok' } });
  });

  it('Renders a "Back to sign in" link pointing to the login route', () => {
    renderPage();

    const backLink = screen.getByRole('link', { name: /Back to sign in/i });
    expect(backLink).toHaveAttribute('href', '/login');
  });
});
