import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import LoginPage from './LoginPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { api } from '@/lib/axios';
import { paths } from '@/routes/paths';

// Mock react-router-dom
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Mock axios API
vi.mock('@/lib/axios', () => ({
  api: {
    post: vi.fn(),
  }
}));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false }
  }
});

const renderLoginPage = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    useAuthStore.setState({ user: null, accessToken: null, isAuthenticated: false });
  });

  afterEach(() => {
    cleanup();
  });

  it('Empty submit → shows both required-field errors, no API call made', async () => {
    renderLoginPage();
    
    const submitBtn = screen.getByRole('button', { name: /Sign in/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Email is required')).toBeInTheDocument();
      expect(screen.getByText('Password is required')).toBeInTheDocument();
    });

    expect(api.post).not.toHaveBeenCalled();
  });

  it('Invalid email format → shows email validation error', async () => {
    renderLoginPage();
    
    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    fireEvent.change(emailInput, { target: { value: 'not-an-email' } });
    
    const submitBtn = screen.getByRole('button', { name: /Sign in/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Enter a valid email address')).toBeInTheDocument();
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Valid submit → mutation fires with correct payload', async () => {
    (api.post as any).mockResolvedValueOnce({ data: { user: { id: '1', email: 'test@saharvest.org' }, accessToken: 'token' } });
    
    renderLoginPage();
    
    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    const passwordInput = screen.getByPlaceholderText('••••••••');
    const submitBtn = screen.getByRole('button', { name: /Sign in/i });

    fireEvent.change(emailInput, { target: { value: 'test@saharvest.org' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/api/auth/login', {
        email: 'test@saharvest.org',
        password: 'password123'
      });
    });
  });

  it('Successful login → authStore.login() called, navigates to dashboard', async () => {
    const mockData = { user: { id: '1', email: 'test@saharvest.org' }, accessToken: 'token' };
    (api.post as any).mockResolvedValueOnce({ data: mockData });
    
    renderLoginPage();
    
    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    const passwordInput = screen.getByPlaceholderText('••••••••');
    const submitBtn = screen.getByRole('button', { name: /Sign in/i });

    fireEvent.change(emailInput, { target: { value: 'test@saharvest.org' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
      expect(mockNavigate).toHaveBeenCalledWith(paths.dashboard);
    });
  });

  it('Failed login (mock 401) → generic error message shown, no navigation happens', async () => {
    (api.post as any).mockRejectedValueOnce({ response: { status: 401 } });
    
    renderLoginPage();
    
    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    const passwordInput = screen.getByPlaceholderText('••••••••');
    const submitBtn = screen.getByRole('button', { name: /Sign in/i });

    fireEvent.change(emailInput, { target: { value: 'test@saharvest.org' } });
    fireEvent.change(passwordInput, { target: { value: 'wrongpass' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Login failed. Please check your credentials or API connection.')).toBeInTheDocument();
    });
    expect(mockNavigate).not.toHaveBeenCalled();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('Submit button disabled and shows loading text while isPending', async () => {
    let resolvePost: any;
    const postPromise = new Promise((resolve) => { resolvePost = resolve; });
    (api.post as any).mockReturnValueOnce(postPromise);
    
    renderLoginPage();
    
    const emailInput = screen.getByPlaceholderText('name@saharvest.org');
    const passwordInput = screen.getByPlaceholderText('••••••••');
    const submitBtn = screen.getByRole('button', { name: /Sign in/i });

    fireEvent.change(emailInput, { target: { value: 'test@saharvest.org' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Signing in…/i })).toBeDisabled();
    });

    resolvePost({ data: { user: { id: '1' }, accessToken: 'token' } });
  });
});
