import { render, screen, cleanup, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';
import { vettingRoutes } from '../routes';
import type { VettingOfficer, VettingSiteVisit } from '../types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }));
vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

const officers: VettingOfficer[] = [
  { id: 'o1', fullName: 'Thandi Mokoena', initials: 'TM', assignedRegion: 'Gauteng North', workloadCount: 2 },
];
const visit: VettingSiteVisit = {
  id: 'v1',
  donorId: 'd1',
  donorCompanyName: 'FoodCorp SA',
  officerId: 'o1',
  officerInitials: 'TM',
  scheduledDate: '2026-10-21',
  scheduledTime: '09:00:00',
  locationLabel: 'Midrand',
  visitNotes: null,
  visitStatus: 'ToDo',
};

function signInAs(roles: string[]) {
  useAuthStore.setState({
    user: { id: 'u1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@saharvest.org', roles },
    isAuthenticated: true,
    isHydrating: false,
  });
}

function renderAt(path: string) {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          {vettingRoutes}
          <Route path={paths.notAuthorized} element={<div>Not authorized page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('vetting pages', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.get).mockImplementation(async (url: string) => {
      if (url === '/api/v1/vetting/officers') return { data: { data: officers } };
      if (url === '/api/v1/vetting/site-visits') return { data: { data: [visit] } };
      throw new Error(`unexpected GET ${url}`);
    });
  });
  afterEach(cleanup);

  it('redirects Procurement away from Officers to notAuthorized', () => {
    signInAs(['Procurement']);
    renderAt(paths.vettingOfficers);
    expect(screen.getByText('Not authorized page')).toBeInTheDocument();
  });

  it('redirects Marketing away from Site visits to notAuthorized', () => {
    signInAs(['Marketing']);
    renderAt(paths.vettingVisits);
    expect(screen.getByText('Not authorized page')).toBeInTheDocument();
  });

  it('shows Admins the officers table with initials chip and read-only workload', async () => {
    signInAs(['Admin']);
    renderAt(paths.vettingOfficers);
    const row = (await screen.findByText('Thandi Mokoena')).closest('tr')!;
    expect(within(row).getByText('TM')).toBeInTheDocument();
    expect(within(row).getByText('Gauteng North')).toBeInTheDocument();
    expect(within(row).getByText('2')).toBeInTheDocument();
  });

  it('uppercases officer initials as they are typed and caps them at 5', async () => {
    const user = userEvent.setup();
    signInAs(['Admin']);
    vi.mocked(api.post).mockResolvedValue({ data: { data: officers[0] } });
    renderAt(paths.vettingOfficers);

    await user.click(await screen.findByRole('button', { name: /new officer/i }));
    await user.type(screen.getByLabelText('Full name'), 'Pieter van Wyk');
    await user.type(screen.getByLabelText('Initials'), 'pvwxyz');
    await user.type(screen.getByLabelText('Assigned region'), 'Western Cape');
    expect(screen.getByLabelText('Initials')).toHaveValue('PVWXY');

    await user.click(screen.getByRole('button', { name: 'Create officer' }));
    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/api/v1/vetting/officers', {
        fullName: 'Pieter van Wyk',
        initials: 'PVWXY',
        assignedRegion: 'Western Cape',
      }),
    );
  });

  it('lists the day’s visits and PATCHes the new status when the select changes', async () => {
    const user = userEvent.setup();
    signInAs(['Procurement']);
    vi.mocked(api.patch).mockResolvedValue({ data: { data: { ...visit, visitStatus: 'InProgress' } } });
    renderAt(paths.vettingVisits);

    const status = await screen.findByLabelText('Status for FoodCorp SA');
    expect(screen.getByText('09:00')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /start vetting/i })).toHaveAttribute(
      'href',
      '/vetting/new?donorId=d1&officerId=o1&siteVisitId=v1',
    );

    await user.selectOptions(status, 'InProgress');

    await waitFor(() =>
      expect(api.patch).toHaveBeenCalledWith('/api/v1/vetting/site-visits/v1', { visitStatus: 'InProgress' }),
    );
  });

  it('hides Start vetting on a completed visit', async () => {
    signInAs(['Procurement']);
    vi.mocked(api.get).mockImplementation(async (url: string) =>
      url === '/api/v1/vetting/officers' ? { data: { data: officers } } : { data: { data: [{ ...visit, visitStatus: 'Completed' }] } },
    );
    renderAt(paths.vettingVisits);
    await screen.findByLabelText('Status for FoodCorp SA');
    expect(screen.queryByRole('link', { name: /start vetting/i })).not.toBeInTheDocument();
  });
});
