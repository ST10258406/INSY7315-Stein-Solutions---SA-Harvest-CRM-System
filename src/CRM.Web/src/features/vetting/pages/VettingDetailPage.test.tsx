import { render, screen, cleanup, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { toast } from 'sonner';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import VettingDetailPage from './VettingDetailPage';
import { VettingForm } from '../components/VettingForm';
import type { EvidencePhotoCategory, VettingSubmission } from '../types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }));
vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

const CATEGORIES: EvidencePhotoCategory[] = ['Kitchen', 'DryStorage', 'FridgeFreezer', 'ToiletsWater'];
const SUBMISSION_URL = '/api/v1/vetting/submissions/s1';

function submission(captured: number, overrides: Partial<VettingSubmission> = {}): VettingSubmission {
  return {
    id: 's1', formReference: 'VET-2026-0007', donorId: 'd1', donorCompanyName: 'FoodCorp SA', officerId: 'o1',
    officerFullName: 'Thandi Mokoena', siteVisitId: null, approvalState: 'New', submittedAt: null, adminDecision: null,
    decisionReason: null, createdAt: '2026-10-20T08:00:00Z', updatedAt: '2026-10-20T08:00:00Z',
    details: { legalName: 'FoodCorp South Africa', isRegisteredNpo: false, coreFocusAreas: ['Feeding', 'Health'] } as VettingSubmission['details'],
    flags: [],
    photos: CATEGORIES.map((category, i) => ({
      shotIndex: i, category, isCaptured: i < captured, fileName: i < captured ? `${category}.jpg` : null,
      capturedAt: i < captured ? '2026-10-20T09:15:00Z' : null,
    })),
    files: { npoCertificate: false, pboCertificate: false, kitchenStoragePhotos: true, facilityPhotos: false, certificates: false },
    ...overrides,
  };
}

function signInAs(roles: string[]) {
  useAuthStore.setState({
    user: { id: 'u1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@saharvest.org', roles },
    isAuthenticated: true,
    isHydrating: false,
  });
}

function renderDetail() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/vetting/s1']}>
        <Routes>
          <Route path="/vetting/:id" element={<VettingDetailPage />} />
          <Route path="/vetting/review" element={<div>Review queue page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const submissionGets = () => vi.mocked(api.get).mock.calls.filter(([url]) => url === SUBMISSION_URL).length;
const photoUrlGets = () => vi.mocked(api.get).mock.calls.filter(([url]) => String(url).includes('/photos/')).length;

describe('VettingDetailPage', () => {
  let current: VettingSubmission;

  beforeEach(() => {
    vi.clearAllMocks();
    current = submission(3);
    vi.mocked(api.get).mockImplementation(async (url: string) => {
      if (url === SUBMISSION_URL) return { data: { data: current } };
      if (url.includes('/photos/')) return { data: { data: { downloadUrl: `https://blob/${url}?sas`, expiresAt: '', originalFileName: 'x.jpg' } } };
      throw new Error(`unexpected GET ${url}`);
    });
  });
  afterEach(cleanup);

  it('shows the captured counter and the missing slot from the API data, and loads thumbnails only once opened', async () => {
    const user = userEvent.setup();
    renderDetail();
    const header = await screen.findByRole('button', { name: /Evidence Photos/ });
    expect(header).toHaveTextContent('3 / 4 captured');

    expect(within(screen.getByTestId('photo-ToiletsWater')).getByText('Missing')).toBeInTheDocument();
    expect(within(screen.getByTestId('photo-Kitchen')).getByText('Captured')).toBeInTheDocument();
    expect(photoUrlGets()).toBe(0);

    await user.click(header);
    await waitFor(() => expect(photoUrlGets()).toBe(3)); // the 3 captured photos, not the missing one
    expect(await screen.findByAltText('Kitchen evidence photo')).toHaveAttribute('src', expect.stringContaining('/photos/0'));
  });

  it('renders read-only values: booleans as Yes/No and arrays as chips', async () => {
    const user = userEvent.setup();
    renderDetail();
    await user.click(await screen.findByRole('button', { name: 'Expand all' }));
    expect(screen.getByText('FoodCorp South Africa')).toBeInTheDocument();
    expect(screen.getByText('Feeding')).toBeInTheDocument();
    expect(screen.getByText('Health')).toBeInTheDocument();
    expect(screen.getAllByText('No').length).toBeGreaterThan(0);
  });

  it('disables Submit and names the missing photos while any is missing', async () => {
    renderDetail();
    const submit = await screen.findByRole('button', { name: /submit for review/i });
    expect(submit).toBeDisabled();
    expect(screen.getByText('Missing photos: Toilets & water')).toBeInTheDocument();
  });

  it('submits once all 4 are captured, then refetches and shows the new auto flags', async () => {
    const user = userEvent.setup();
    current = submission(4);
    vi.mocked(api.post).mockImplementation(async () => {
      current = submission(4, {
        approvalState: 'Ready',
        submittedAt: '2026-10-20T10:00:00Z',
        flags: [{ id: 'f1', description: 'Organisation is not a registered NPO', source: 'Auto', createdAt: '2026-10-20T10:00:00Z' }],
      });
      return { data: { data: current } };
    });
    renderDetail();

    const submit = await screen.findByRole('button', { name: /submit for review/i });
    expect(submit).toBeEnabled();
    const before = submissionGets();
    await user.click(submit);
    await user.click(screen.getByRole('button', { name: 'Submit' }));

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/api/v1/vetting/submissions/s1/submit'));
    await waitFor(() => expect(submissionGets()).toBeGreaterThan(before));
    expect(await screen.findByText('Organisation is not a registered NPO')).toBeInTheDocument();
    expect(toast.success).toHaveBeenCalledWith('VET-2026-0007 submitted for review.');
    expect(screen.queryByRole('button', { name: /submit for review/i })).not.toBeInTheDocument();
  });

  it('shows the API message when submit still fails with a 400', async () => {
    const user = userEvent.setup();
    current = submission(4);
    vi.mocked(api.post).mockRejectedValue({
      response: { status: 400, data: { message: 'One or more validation errors occurred.', errors: [{ field: 'Photos', message: 'Missing evidence photos: Kitchen.' }] } },
    });
    renderDetail();
    await user.click(await screen.findByRole('button', { name: /submit for review/i }));
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    expect(await screen.findByText('Missing evidence photos: Kitchen.')).toBeInTheDocument();
  });
});

describe('VettingDetailPage — admin review', () => {
  let current: VettingSubmission;
  const ready = () => submission(4, { approvalState: 'Ready', submittedAt: '2026-10-20T10:00:00Z' });

  beforeEach(() => {
    vi.clearAllMocks();
    current = ready();
    vi.mocked(api.get).mockImplementation(async (url: string) => {
      if (url === SUBMISSION_URL) return { data: { data: current } };
      return { data: { data: { downloadUrl: 'https://blob/x?sas', expiresAt: '', originalFileName: 'x.jpg' } } };
    });
  });
  afterEach(() => {
    cleanup();
    useAuthStore.setState({ user: null, isAuthenticated: false });
  });

  const decisionButtons = () => ['Approve', 'Return to officer', 'Decline'].map((name) => screen.queryByRole('button', { name: new RegExp(`^${name}$`) }));

  it.each([
    [['Procurement'], 'Ready', false],
    [['Admin'], 'New', false],
    [['Admin'], 'Approved', false],
    [['Admin'], 'Ready', true],
    [['SuperAdmin'], 'Ready', true],
  ] as const)('roles %j on %s → decision buttons shown: %s', async (roles, approvalState, shown) => {
    signInAs([...roles]);
    current = submission(4, { approvalState, submittedAt: approvalState === 'New' ? null : '2026-10-20T10:00:00Z' });
    renderDetail();
    await screen.findByText('VET-2026-0007');
    for (const button of decisionButtons()) expect(!!button).toBe(shown);
    expect(!!screen.queryByLabelText('New reviewer flag')).toBe(shown);
  });

  it('does not submit Decline without a reason', async () => {
    const user = userEvent.setup();
    signInAs(['Admin']);
    renderDetail();
    await user.click(await screen.findByRole('button', { name: /^Decline$/ }));
    expect(screen.getByText(/This closes the vetting/)).toBeInTheDocument();
    await user.type(screen.getByLabelText(/Reason for declining/), '   ');
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Decline' }));

    expect(await screen.findByText('Give a reason so the officer knows what happened')).toBeInTheDocument();
    expect(api.post).not.toHaveBeenCalled();
  });

  it('Return sends the decision and the trimmed reason, then goes back to the queue', async () => {
    const user = userEvent.setup();
    signInAs(['Admin']);
    vi.mocked(api.post).mockResolvedValue({ data: { data: { ...ready(), approvalState: 'Returned' } } });
    renderDetail();
    await user.click(await screen.findByRole('button', { name: /^Return to officer$/ }));
    expect(screen.getByText(/The officer will see this reason and can correct and resubmit/)).toBeInTheDocument();
    await user.type(screen.getByLabelText(/What needs correcting/), '  Toilets need re-inspection ');
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Return to officer' }));

    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/api/v1/vetting/submissions/s1/decision', { decision: 'Return', reason: 'Toilets need re-inspection' }),
    );
    expect(await screen.findByText('Review queue page')).toBeInTheDocument();
    expect(toast.success).toHaveBeenCalledWith('VET-2026-0007 returned to the officer.');
  });

  it('Approve needs no reason', async () => {
    const user = userEvent.setup();
    signInAs(['Admin']);
    vi.mocked(api.post).mockResolvedValue({ data: { data: { ...ready(), approvalState: 'Approved' } } });
    renderDetail();
    await user.click(await screen.findByRole('button', { name: /^Approve$/ }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Approve' }));
    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/api/v1/vetting/submissions/s1/decision', { decision: 'Approve' }));
  });

  it('a 409 says another admin already reviewed it and refetches', async () => {
    const user = userEvent.setup();
    signInAs(['Admin']);
    vi.mocked(api.post).mockRejectedValue({ response: { status: 409, data: { message: 'Only a submission that is Ready…' } } });
    renderDetail();
    await user.click(await screen.findByRole('button', { name: /^Approve$/ }));
    const before = submissionGets();
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Approve' }));

    expect(await screen.findByText('This submission was already reviewed.')).toBeInTheDocument();
    await waitFor(() => expect(submissionGets()).toBeGreaterThan(before));
  });

  it('Add flag posts the description and the new flag shows as Reviewer', async () => {
    const user = userEvent.setup();
    signInAs(['Admin']);
    vi.mocked(api.post).mockImplementation(async () => {
      current = { ...ready(), flags: [{ id: 'f9', description: 'Fridge not working', source: 'Reviewer', createdAt: '2026-10-20T11:00:00Z' }] };
      return { data: { data: current.flags[0] } };
    });
    renderDetail();
    await user.type(await screen.findByLabelText('New reviewer flag'), 'Fridge not working');
    await user.click(screen.getByRole('button', { name: /add flag/i }));

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/api/v1/vetting/submissions/s1/flags', { description: 'Fridge not working' }));
    const flag = (await screen.findByText('Fridge not working')).closest('li')!;
    expect(within(flag).getByText('Reviewer')).toBeInTheDocument();
  });

  it('shows the decision and reason in a summary card once decided', async () => {
    current = submission(4, { approvalState: 'Declined', adminDecision: 'Decline', decisionReason: 'Not a food-relief organisation' });
    renderDetail();
    const summary = await screen.findByRole('status');
    expect(summary).toHaveTextContent('Decision: Decline (declined)');
    expect(summary).toHaveTextContent('Not a food-relief organisation');
  });
});

describe('VettingForm create mode uploads', () => {
  afterEach(cleanup);

  it('shows the Save first placeholder instead of upload controls', async () => {
    const user = userEvent.setup();
    vi.mocked(api.get).mockResolvedValue({ data: { data: [] } });
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <MemoryRouter>
          <VettingForm mode="create" />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    await user.click(screen.getByRole('button', { name: 'Expand all' }));
    // NPO, PBO, kitchen, facility, certificates + the evidence photos.
    expect(screen.getAllByText('Save the form first to add files and photos.')).toHaveLength(6);
    expect(screen.queryByLabelText(/file input/)).not.toBeInTheDocument();
  });
});
