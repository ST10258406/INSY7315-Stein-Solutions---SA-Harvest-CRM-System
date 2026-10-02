// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { render, screen, cleanup } from '@testing-library/react';
import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { useAuthStore } from '@/store/authStore';

function LocationDisplay() {
  const location = useLocation();
  return <div data-testid="location-display">{location.pathname}</div>;
}

describe('ProtectedRoute Component', () => {
  afterEach(cleanup);
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      accessToken: null,
      isAuthenticated: false,
      isHydrating: false,
    });
  });

  it('renders nothing/spinner when isHydrating is true', () => {
    useAuthStore.setState({ isHydrating: true, isAuthenticated: false });

    const { container } = render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute><div data-testid="protected-content">Content</div></ProtectedRoute>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByTestId('protected-content')).toBeNull();
    expect(container.firstChild).toBeNull();
  });

  it('redirects to /login when unauthenticated and done hydrating', () => {
    useAuthStore.setState({ isHydrating: false, isAuthenticated: false });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute><div data-testid="protected-content">Content</div></ProtectedRoute>} />
          <Route path="/login" element={<LocationDisplay />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByTestId('protected-content')).toBeNull();
    expect(screen.getByTestId('location-display').textContent).toBe('/login');
  });

  it('renders children when authenticated and no allowedRoles prop provided', () => {
    useAuthStore.setState({
      isHydrating: false,
      isAuthenticated: true,
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['User'] },
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute><div data-testid="protected-content">Content</div></ProtectedRoute>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByTestId('protected-content')).toBeDefined();
  });

  it('redirects to /not-authorized when authenticated but wrong role', () => {
    useAuthStore.setState({
      isHydrating: false,
      isAuthenticated: true,
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['User'] },
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute allowedRoles={['Admin']}><div data-testid="protected-content">Content</div></ProtectedRoute>} />
          <Route path="/not-authorized" element={<LocationDisplay />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByTestId('protected-content')).toBeNull();
    expect(screen.getByTestId('location-display').textContent).toBe('/not-authorized');
  });

  it('redirects a user on a temporary password to /change-password', () => {
    useAuthStore.setState({
      isHydrating: false,
      isAuthenticated: true,
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['Admin'], mustChangePassword: true },
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute><div data-testid="protected-content">Content</div></ProtectedRoute>} />
          <Route path="/change-password" element={<LocationDisplay />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByTestId('protected-content')).toBeNull();
    expect(screen.getByTestId('location-display').textContent).toBe('/change-password');
  });

  it('lets a user on a temporary password reach /change-password itself', () => {
    useAuthStore.setState({
      isHydrating: false,
      isAuthenticated: true,
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['Admin'], mustChangePassword: true },
    });

    render(
      <MemoryRouter initialEntries={['/change-password']}>
        <Routes>
          <Route path="/change-password" element={<ProtectedRoute><div data-testid="protected-content">Content</div></ProtectedRoute>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByTestId('protected-content')).toBeDefined();
  });

  it('renders children when authenticated with correct role', () => {
    useAuthStore.setState({
      isHydrating: false,
      isAuthenticated: true,
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['Admin'] },
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/protected" element={<ProtectedRoute allowedRoles={['Admin']}><div data-testid="protected-content">Content</div></ProtectedRoute>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByTestId('protected-content')).toBeDefined();
  });
});
