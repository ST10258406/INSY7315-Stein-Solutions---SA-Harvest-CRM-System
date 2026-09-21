import { render, screen, waitFor, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { api } from '@/lib/axios';
import DonorListPage from './DonorListPage';
import type { DonorListItemDto, PaginatedResult } from './types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

function donor(overrides: Partial<DonorListItemDto> = {}): DonorListItemDto {
  return {
    id: 'donor-1',
    companyName: 'Acme Co',
    companyType: 'Retailer',
    status: 'Active',
    submissionSource: 'ManualCapture',
    relationshipManager: null,
    followUpDate: null,
    lastInteractionDate: null,
    lastInteractionType: null,
    operationalRegions: [],
    donationFrequency: null,
    donationTypes: [],
    ...overrides,
  };
}

function paginatedResult(data: DonorListItemDto[], overrides: Partial<PaginatedResult<DonorListItemDto>['pagination']> = {}): PaginatedResult<DonorListItemDto> {
  return {
    data,
    pagination: { page: 1, pageSize: 20, totalCount: data.length, totalPages: 1, ...overrides },
  };
}

// Every lookup dropdown fires its own GET on mount — resolve them all so the
// filters bar renders without dangling pending promises, and route the
// donors-list GET to whatever the test configures.
function mockApi(donorsResponse: () => Promise<{ data: PaginatedResult<DonorListItemDto> }>) {
  vi.mocked(api.get).mockImplementation((url: string) => {
    if (url === '/api/v1/donors') return donorsResponse();
    return Promise.resolve({ data: { data: [] } });
  });
}

function renderPage(initialEntry = '/donors') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/donors" element={<DonorListPage />} />
          <Route path="/donors/:id" element={<div data-testid="donor-detail">Detail page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('DonorListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  it('renders donor rows returned by the API', async () => {
    mockApi(() =>
      Promise.resolve({
        data: paginatedResult([
          donor({ id: 'd1', companyName: 'Acme Co' }),
          donor({ id: 'd2', companyName: 'Beta Foods', status: 'PendingReview' }),
        ]),
      })
    );

    renderPage();

    expect(await screen.findByText('Acme Co')).toBeInTheDocument();
    expect(screen.getByText('Beta Foods')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Pending review' })).toBeInTheDocument();
  });

  it('opens the send-public-form invite dialog, prefilled with a default subject and message', async () => {
    const user = userEvent.setup();
    mockApi(() => Promise.resolve({ data: paginatedResult([]) }));

    renderPage();
    await screen.findByText(/donors total|No donors match/i);

    await user.click(screen.getByRole('button', { name: /Send Public Form/i }));

    expect(await screen.findByRole('heading', { name: 'Send public form' })).toBeInTheDocument();
    expect(screen.getByLabelText('Subject')).toHaveValue("You're invited to become an SA Harvest donor");
    expect(screen.getByLabelText('To')).toHaveValue('');
  });

  it('shows an empty state when no donors match', async () => {
    mockApi(() => Promise.resolve({ data: paginatedResult([]) }));

    renderPage();

    expect(await screen.findByText('No donors found')).toBeInTheDocument();
  });

  it('shows an error state with a retry action on failure', async () => {
    mockApi(() => Promise.reject({ isAxiosError: true, response: { data: { message: 'Server exploded' } } }));

    renderPage();

    expect(await screen.findByText("Couldn't load donors")).toBeInTheDocument();
    expect(screen.getByText('Server exploded')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });

  it('reads initial filters from the URL and sends them to the API', async () => {
    mockApi(() => Promise.resolve({ data: paginatedResult([donor()]) }));

    renderPage('/donors?status=Active&search=Acme&page=2');

    await waitFor(() =>
      expect(api.get).toHaveBeenCalledWith(
        '/api/v1/donors',
        expect.objectContaining({ params: expect.objectContaining({ status: 'Active', search: 'Acme', page: 2 }) })
      )
    );
  });

  it('clicking a sortable column header requests that sort field', async () => {
    const user = userEvent.setup();
    mockApi(() => Promise.resolve({ data: paginatedResult([donor()]) }));

    renderPage();
    await screen.findByText('Acme Co');

    await user.click(screen.getByRole('button', { name: /company/i }));

    await waitFor(() =>
      expect(api.get).toHaveBeenLastCalledWith(
        '/api/v1/donors',
        expect.objectContaining({ params: expect.objectContaining({ sortBy: 'companyName', sortDir: 'asc' }) })
      )
    );
  });

  it('navigates to the donor detail page when a row is clicked', async () => {
    const user = userEvent.setup();
    mockApi(() => Promise.resolve({ data: paginatedResult([donor({ id: 'donor-42', companyName: 'Acme Co' })]) }));

    renderPage();
    const row = (await screen.findByText('Acme Co')).closest('tr')!;

    await user.click(row);

    expect(await screen.findByTestId('donor-detail')).toBeInTheDocument();
  });

  it('shows pagination controls and requests the next page on click', async () => {
    const user = userEvent.setup();
    mockApi(() =>
      Promise.resolve({
        data: paginatedResult([donor()], { page: 1, pageSize: 20, totalCount: 45, totalPages: 3 }),
      })
    );

    renderPage();

    expect(await screen.findByText('Showing 1–20 of 45 donors')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next page' }));

    await waitFor(() =>
      expect(api.get).toHaveBeenLastCalledWith(
        '/api/v1/donors',
        expect.objectContaining({ params: expect.objectContaining({ page: 2 }) })
      )
    );
  });
});
