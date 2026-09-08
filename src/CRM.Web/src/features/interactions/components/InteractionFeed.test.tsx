import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { api } from '@/lib/axios';
import { InteractionFeed } from './InteractionFeed';
import type { InteractionLogDto, PaginatedResult } from '../types';

vi.mock('@/lib/axios', () => ({
  api: { get: vi.fn(), post: vi.fn() },
}));

const row: InteractionLogDto = {
  id: 'int-1',
  donorId: 'donor-1',
  interactionType: 'Call',
  subject: 'Surplus volume forecast for Q3',
  body: 'Dineo confirmed the plant will have chilled surplus through September.',
  emailAttachmentUrl: null,
  createdAt: new Date(Date.now() - 2 * 86_400_000).toISOString(),
  createdBy: { id: 'user-1', fullName: 'Nomsa Khumalo' },
};

const onePage: PaginatedResult<InteractionLogDto> = {
  data: [row],
  pagination: { page: 1, pageSize: 20, totalCount: 1, totalPages: 1 },
};

function renderFeed() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <InteractionFeed donorId="donor-1" />
    </QueryClientProvider>,
  );
}

describe('InteractionFeed', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(() => cleanup());

  it('renders a card per interaction with subject, type tag and author', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: onePage });
    renderFeed();

    expect(await screen.findByText('Surplus volume forecast for Q3')).toBeInTheDocument();
    expect(screen.getByText('Call')).toBeInTheDocument();
    expect(screen.getByText('Nomsa Khumalo')).toBeInTheDocument();
  });

  it('shows the empty state when the donor has no interactions', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({
      data: { data: [], pagination: { page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } },
    });
    renderFeed();

    expect(await screen.findByText('No interactions logged yet')).toBeInTheDocument();
  });

  it('opens the log-interaction dialog from the header button', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: onePage });
    renderFeed();
    await screen.findByText('Surplus volume forecast for Q3');

    await userEvent.click(screen.getByRole('button', { name: /log interaction/i }));

    expect(await screen.findByRole('heading', { name: 'Log interaction' })).toBeInTheDocument();
  });
});
