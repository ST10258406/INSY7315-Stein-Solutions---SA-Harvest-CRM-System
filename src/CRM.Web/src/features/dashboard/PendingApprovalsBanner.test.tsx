import { render, screen, waitFor, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { PendingApprovalsBanner } from './PendingApprovalsBanner';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));

const countResponse = (totalCount: number) => ({
  data: { data: [], pagination: { page: 1, pageSize: 1, totalCount, totalPages: totalCount ? 1 : 0 } },
});

function renderBanner() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <PendingApprovalsBanner />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function setRoles(roles: string[]) {
  useAuthStore.setState({ user: { id: 'u1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles } });
}

describe('PendingApprovalsBanner', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders the banner with a link to approvals when an admin has pending approvals', async () => {
    setRoles(['Admin']);
    vi.mocked(api.get).mockResolvedValueOnce(countResponse(4));
    renderBanner();

    const link = await screen.findByRole('link', { name: /waiting for review/i });
    expect(link).toHaveAttribute('href', '/approvals');
    expect(screen.getByText(/4 donor submissions are waiting for review/i)).toBeInTheDocument();
    expect(api.get).toHaveBeenCalledWith('/api/v1/approvals', {
      params: { status: 'Pending', page: 1, pageSize: 1 },
    });
  });

  it('renders nothing for an admin when there are no pending approvals', async () => {
    setRoles(['Admin']);
    vi.mocked(api.get).mockResolvedValueOnce(countResponse(0));
    const { container } = render(
      <QueryClientProvider client={new QueryClient()}>
        <MemoryRouter>
          <PendingApprovalsBanner />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    await waitFor(() => expect(api.get).toHaveBeenCalled());
    expect(container).toBeEmptyDOMElement();
  });

  it('renders nothing (and makes no request) for a non-admin', () => {
    setRoles(['Procurement']);
    renderBanner();
    expect(screen.queryByRole('link')).not.toBeInTheDocument();
    expect(api.get).not.toHaveBeenCalled();
  });
});
