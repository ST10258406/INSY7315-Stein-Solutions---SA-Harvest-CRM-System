import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import ResetPasswordPage from './ResetPasswordPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { api } from '@/lib/axios';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

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

const VALID_LINK = '/reset-password?token=abc123&email=test%40saharvest.org';

const renderPage = (initialEntry: string = VALID_LINK) => {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <ResetPasswordPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
};

const fillAndSubmit = (newPassword: string, confirmPassword: string) => {
  fireEvent.change(screen.getAllByPlaceholderText('••••••••')[0], { target: { value: newPassword } });
  fireEvent.change(screen.getAllByPlaceholderText('••••••••')[1], { target: { value: confirmPassword } });
  fireEvent.click(screen.getByRole('button', { name: /Reset password/i }));
};

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('Missing token/email in URL → shows invalid link message, no form rendered', () => {
    renderPage('/reset-password');

    expect(screen.getByText(/This reset link is invalid or has expired/i)).toBeInTheDocument();
    expect(screen.queryByPlaceholderText('••••••••')).not.toBeInTheDocument();
  });

  it('Empty submit → shows both required-field errors, no API call made', async () => {
    renderPage();

    fireEvent.click(screen.getByRole('button', { name: /Reset password/i }));

    await waitFor(() => {
      expect(screen.getByText('Password must be at least 8 characters')).toBeInTheDocument();
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Weak password → shows password rule validation error', async () => {
    renderPage();

    fillAndSubmit('weakpass', 'weakpass');

    await waitFor(() => {
      expect(screen.getByText('Password must contain at least one digit')).toBeInTheDocument();
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Mismatched passwords → shows confirm-password error, no API call made', async () => {
    renderPage();

    fillAndSubmit('Str0ng!Pass', 'Different1!');

    await waitFor(() => {
      expect(screen.getByText('Passwords do not match')).toBeInTheDocument();
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Valid submit → mutation fires with token/email from URL plus form values', async () => {
    (api.post as any).mockResolvedValueOnce({ data: { message: 'Password has been reset successfully.' } });

    renderPage();
    fillAndSubmit('Str0ng!Pass', 'Str0ng!Pass');

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/api/auth/reset-password', {
        newPassword: 'Str0ng!Pass',
        confirmPassword: 'Str0ng!Pass',
        token: 'abc123',
        email: 'test@saharvest.org',
      });
    });
  });

  it('Successful reset → shows success message; clicking continue navigates to login', async () => {
    (api.post as any).mockResolvedValueOnce({ data: { message: 'Password has been reset successfully.' } });

    renderPage();
    fillAndSubmit('Str0ng!Pass', 'Str0ng!Pass');

    await waitFor(() => {
      expect(screen.getByText(/Your password has been reset/i)).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /Continue to sign in/i }));
    expect(mockNavigate).toHaveBeenCalledWith('/login');
  });

  it('Failed reset with invalid/expired token → shows invalid-link message', async () => {
    (api.post as any).mockRejectedValueOnce({
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          status: 400,
          code: 'VALIDATION_ERROR',
          message: 'One or more validation errors occurred.',
          errors: [{ field: 'Token', message: 'This reset token is invalid or has expired.' }],
          traceId: 'trace-1',
        },
      },
    });

    renderPage();
    fillAndSubmit('Str0ng!Pass', 'Str0ng!Pass');

    await waitFor(() => {
      expect(screen.getByText('This reset link is invalid or has expired.')).toBeInTheDocument();
    });
  });

  it('Failed reset with generic/network error → shows generic error message', async () => {
    (api.post as any).mockRejectedValueOnce(new Error('Network Error'));

    renderPage();
    fillAndSubmit('Str0ng!Pass', 'Str0ng!Pass');

    await waitFor(() => {
      expect(screen.getByText('Something went wrong. Please try again.')).toBeInTheDocument();
    });
  });

  it('Submit button disabled and shows loading text while isPending', async () => {
    let resolvePost: any;
    const postPromise = new Promise((resolve) => {
      resolvePost = resolve;
    });
    (api.post as any).mockReturnValueOnce(postPromise);

    renderPage();
    fillAndSubmit('Str0ng!Pass', 'Str0ng!Pass');

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Resetting…/i })).toBeDisabled();
    });

    resolvePost({ data: { message: 'ok' } });
  });
});
