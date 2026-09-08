import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { api } from '@/lib/axios';
import { OverdueFollowUpsWidget } from './OverdueFollowUpsWidget';
import type { DonorListItemDto, PaginatedResult } from '@/features/donors/types';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));

function donor(overrides: Partial<DonorListItemDto> = {}): DonorListItemDto {
  return {
    id: 'donor-1',
    companyName: 'Cape Fresh Produce Co.',
    companyType: 'Distributor',
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

function result(rows: DonorListItemDto[], totalCount = rows.length): PaginatedResult<DonorListItemDto> {
  return { data: rows, pagination: { page: 1, pageSize: 6, totalCount, totalPages: Math.ceil(totalCount / 6) } };
}

function renderWidget() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <OverdueFollowUpsWidget />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/** Local calendar date N days ago, as yyyy-MM-dd (matches the API's date-only value). */
const daysAgoDate = (days: number) => {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

describe('OverdueFollowUpsWidget', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('queries donors with a past follow-up date and lists them with days overdue', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({
      data: result([donor({ followUpDate: daysAgoDate(5) })]),
    });

    renderWidget();

    expect(await screen.findByRole('link', { name: 'Cape Fresh Produce Co.' })).toHaveAttribute(
      'href',
      '/donors/donor-1',
    );
    expect(screen.getByText('5 days')).toBeInTheDocument();

    const call = vi.mocked(api.get).mock.calls[0];
    expect(call[0]).toBe('/api/v1/donors');
    expect(call[1]?.params).toMatchObject({ sortBy: 'followUpDate', sortDir: 'asc', pageSize: 6 });
    expect(call[1]?.params.followUpBefore).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('shows the empty state when nothing is overdue', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: result([]) });
    renderWidget();
    expect(await screen.findByText('Nothing overdue')).toBeInTheDocument();
  });

  it('links to the filtered donor list when there are more than the row limit', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({
      data: result([donor({ followUpDate: daysAgoDate(2) })], 20),
    });
    renderWidget();
    expect(await screen.findByRole('link', { name: /view all 20/i })).toBeInTheDocument();
  });
});
