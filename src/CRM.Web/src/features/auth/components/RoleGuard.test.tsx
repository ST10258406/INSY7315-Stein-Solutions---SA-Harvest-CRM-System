// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { render, screen, cleanup } from '@testing-library/react';
import { RoleGuard } from './RoleGuard';
import { useAuthStore } from '@/store/authStore';

describe('RoleGuard Component', () => {
  afterEach(cleanup);
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isHydrating: false,
    });
  });

  it('renders children when user has an allowed role', () => {
    useAuthStore.setState({
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['Admin'] },
      isAuthenticated: true,
      isHydrating: false,
    });

    render(
      <RoleGuard allowedRoles={['Admin', 'SuperAdmin']}>
        <div data-testid="child-content">Authorized Content</div>
      </RoleGuard>
    );

    expect(screen.getByTestId('child-content')).toBeDefined();
  });

  it('renders fallback when user does not have an allowed role', () => {
    useAuthStore.setState({
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['User'] },
      isAuthenticated: true,
      isHydrating: false,
    });

    render(
      <RoleGuard allowedRoles={['Admin', 'SuperAdmin']} fallback={<div data-testid="fallback-content">Fallback Content</div>}>
        <div data-testid="child-content">Authorized Content</div>
      </RoleGuard>
    );

    expect(screen.queryByTestId('child-content')).toBeNull();
    expect(screen.getByTestId('fallback-content')).toBeDefined();
  });

  it('renders nothing when user does not have an allowed role and no fallback provided', () => {
    useAuthStore.setState({
      user: { id: '1', firstName: 'Test', lastName: 'User', email: 'test@example.com', roles: ['User'] },
      isAuthenticated: true,
      isHydrating: false,
    });

    const { container } = render(
      <RoleGuard allowedRoles={['Admin', 'SuperAdmin']}>
        <div data-testid="child-content">Authorized Content</div>
      </RoleGuard>
    );

    expect(screen.queryByTestId('child-content')).toBeNull();
    expect(container.firstChild).toBeNull();
  });
});
