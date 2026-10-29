import { render, screen, cleanup, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { HelpDrawer } from './HelpDrawer';
import { buildProblemReportBody } from '../content';

function renderAs(roles: string[], route = '/donors') {
  useAuthStore.setState({
    user: { id: '1', firstName: 'Thandi', lastName: 'Nkosi', email: 'thandi@saharvest.org', roles },
    accessToken: 'secret-access-token',
  } as Partial<ReturnType<typeof useAuthStore.getState>>);
  render(
    <MemoryRouter initialEntries={[route]}>
      <HelpDrawer open onOpenChange={() => {}} />
    </MemoryRouter>,
  );
}

const topic = (name: string) => screen.queryByRole('button', { name });

describe('HelpDrawer', () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllEnvs();
  });

  it('hides Admin and SuperAdmin topics from Marketing', () => {
    renderAs(['Marketing']);
    expect(topic('Creating and completing tasks')).toBeInTheDocument();
    expect(topic('Reviewing donor approvals')).not.toBeInTheDocument();
    expect(topic('Running and exporting reports')).not.toBeInTheDocument();
    expect(topic('Finding and filtering donors')).not.toBeInTheDocument();
    expect(topic('Using the CRM assistant')).not.toBeInTheDocument();
  });

  it('shows every topic to SuperAdmin', () => {
    renderAs(['SuperAdmin']);
    expect(topic('Reviewing donor approvals')).toBeInTheDocument();
    expect(topic('Using the CRM assistant')).toBeInTheDocument();
  });

  it('filters topics by title and step text', () => {
    renderAs(['Admin']);
    fireEvent.change(screen.getByRole('searchbox', { name: 'Search help topics' }), { target: { value: 'excel' } });
    expect(topic('Running and exporting reports')).toBeInTheDocument();
    expect(topic('Creating and completing tasks')).not.toBeInTheDocument();
  });

  it('expands a topic with aria-expanded and links to its page', () => {
    renderAs(['Admin']);
    const button = topic('Reviewing donor approvals')!;
    expect(button).toHaveAttribute('aria-expanded', 'false');
    fireEvent.click(button);
    expect(button).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('link', { name: 'Go there' })).toHaveAttribute('href', '/approvals');
  });

  it('hides the support links when VITE_SUPPORT_EMAIL is not set', () => {
    vi.stubEnv('VITE_SUPPORT_EMAIL', '');
    renderAs(['Admin']);
    expect(screen.queryByRole('link', { name: 'Contact support' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Report a problem' })).not.toBeInTheDocument();
  });

  it('builds a problem report with page and version but no personal data', () => {
    vi.stubEnv('VITE_SUPPORT_EMAIL', 'support@saharvest.org');
    vi.stubEnv('VITE_APP_VERSION', 'abc123');
    renderAs(['Admin'], '/reports');

    expect(screen.getByRole('link', { name: 'Contact support' })).toHaveAttribute('href', 'mailto:support@saharvest.org');
    const href = decodeURIComponent(screen.getByRole('link', { name: 'Report a problem' }).getAttribute('href')!);
    expect(href).toContain('Page: /reports');
    expect(href).toContain('App version: abc123');
    expect(href).toContain('Role: Admin');
    for (const secret of ['Thandi', 'Nkosi', 'thandi@saharvest.org', 'secret-access-token']) {
      expect(href).not.toContain(secret);
    }
  });

  it('report body only contains the fields it is given', () => {
    expect(buildProblemReportBody('/tasks', '1.0', 'jsdom', 'Procurement')).toBe(
      'Page: /tasks\nApp version: 1.0\nBrowser: jsdom\nRole: Procurement\n\nWhat happened:\n\nWhat did you expect to happen:\n',
    );
  });
});
