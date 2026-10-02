import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { toast } from 'sonner';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';
import { useLogout, LOGOUT_FAILED_MESSAGE } from './useLogout';

const navigate = vi.fn();

vi.mock('react-router-dom', () => ({ useNavigate: () => navigate }));
vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));
vi.mock('sonner', () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

function signIn() {
  useAuthStore.setState({
    user: { id: 'u1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@saharvest.org', roles: ['Admin'] },
    accessToken: 'access-token',
    isAuthenticated: true,
    isHydrating: false,
  });
}

describe('useLogout', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    signIn();
  });

  it('clears the session and goes to login once the API confirms', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: null });
    const { result } = renderHook(() => useLogout(), { wrapper });

    result.current.mutate();

    await waitFor(() => expect(navigate).toHaveBeenCalledWith(paths.login, { replace: true }));
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('stays signed in and reports the failure when the request never gets a real answer', async () => {
    // e.g. offline: the HttpOnly cookie is still live, so pretending to log out would let the
    // next reload sign the user straight back in.
    vi.mocked(api.post).mockRejectedValue(new Error('Network Error'));
    const { result } = renderHook(() => useLogout(), { wrapper });

    result.current.mutate();

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith(LOGOUT_FAILED_MESSAGE));
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('treats an already-dead session as signed out without an error', async () => {
    // The 401 interceptor already failed to refresh and signed the user out.
    vi.mocked(api.post).mockImplementation(async () => {
      useAuthStore.getState().logout();
      throw new Error('401');
    });
    const { result } = renderHook(() => useLogout(), { wrapper });

    result.current.mutate();

    await waitFor(() => expect(navigate).toHaveBeenCalledWith(paths.login, { replace: true }));
    expect(toast.error).not.toHaveBeenCalled();
  });
});
