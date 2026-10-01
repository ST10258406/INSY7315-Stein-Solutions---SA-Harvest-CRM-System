import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { DonorKpiCards } from './DonorKpiCards';
import type { DonorListItemDto, PaginatedResult } from '@/features/donors/types';

vi.mock('@/lib/axios', () => ({
  api: {
    get: vi.fn(),
  },
}));

const donorListItem: DonorListItemDto = {
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
};

function paginatedResult(totalCount: number): PaginatedResult<DonorListItemDto> {
  return {
    data: totalCount > 0 ? [donorListItem] : [],
    pagination: { page: 1, pageSize: 1, totalCount, totalPages: totalCount },
  };
}

function renderCards() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DonorKpiCards />
    </QueryClientProvider>
  );
}

describe('DonorKpiCards', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests a per-status count via GetDonors with pageSize=1 rather than fetching the full list', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: paginatedResult(0) });

    renderCards();

    await waitFor(() => expect(api.get).toHaveBeenCalledTimes(4));
    expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { status: undefined, pageSize: 1 } });
    expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { status: 'Active', pageSize: 1 } });
    expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { status: 'PendingReview', pageSize: 1 } });
    expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { status: 'Lapsed', pageSize: 1 } });
  });

  it('renders real totalCount values from the API for each card', async () => {
    vi.mocked(api.get).mockImplementation((_url, config) => {
      const status = (config?.params as { status?: string } | undefined)?.status;
      const totals: Record<string, number> = { undefined: 42, Active: 30, PendingReview: 8, Lapsed: 4 };
      return Promise.resolve({ data: paginatedResult(totals[String(status)]) });
    });

    renderCards();

    expect(await screen.findByText('42')).toBeInTheDocument();
    expect(await screen.findByText('30')).toBeInTheDocument();
    expect(await screen.findByText('8')).toBeInTheDocument();
    expect(await screen.findByText('4')).toBeInTheDocument();
  });

  it('renders zero donors without crashing', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: paginatedResult(0) });

    renderCards();

    const zeros = await screen.findAllByText('0');
    expect(zeros).toHaveLength(4);
  });

  it('shows a placeholder dash for a card whose count fails to load, without crashing the others', async () => {
    vi.mocked(api.get).mockImplementation((_url, config) => {
      const status = (config?.params as { status?: string } | undefined)?.status;
      if (status === 'Lapsed') {
        return Promise.reject(new Error('network error'));
      }
      return Promise.resolve({ data: paginatedResult(5) });
    });

    renderCards();

    expect((await screen.findAllByText('5')).length).toBeGreaterThan(0);
    expect(await screen.findByTitle("Couldn't load this count")).toBeInTheDocument();
  });
});
