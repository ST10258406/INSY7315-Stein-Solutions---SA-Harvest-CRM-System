import { render, screen, cleanup, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { QuickSearchDialog } from './QuickSearchDialog';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));

const DONOR_ID = '11111111-1111-1111-1111-111111111111';

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname + location.search}</div>;
}

function renderAs(roles: string[]) {
  useAuthStore.setState({ user: { id: '1', firstName: 'A', lastName: 'B', email: 'a@b.c', roles } } as Partial<
    ReturnType<typeof useAuthStore.getState>
  >);
  render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <QuickSearchDialog open onOpenChange={() => {}} />
        <LocationProbe />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

const input = () => screen.getByRole('combobox', { name: 'Search' });
const type = (value: string) => fireEvent.change(input(), { target: { value } });
const location = () => screen.getByTestId('location').textContent;

describe('QuickSearchDialog', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockResolvedValue({
      data: {
        data: [{ id: DONOR_ID, companyName: 'Woolworths', status: 'Active' }],
        pagination: { page: 1, pageSize: 5, totalCount: 1, totalPages: 1 },
      },
    });
  });

  afterEach(() => {
    cleanup();
    vi.mocked(api.get).mockReset();
  });

  it('sends no donor request for fewer than 2 characters', async () => {
    renderAs(['Procurement']);
    type('w');
    await new Promise((r) => setTimeout(r, 400));
    expect(api.get).not.toHaveBeenCalled();
    expect(screen.getByText('Type at least 2 characters to search donors.')).toBeInTheDocument();
  });

  it('debounces typing into a single donor request', async () => {
    renderAs(['Procurement']);
    type('wo');
    type('woo');
    type('wool');
    await screen.findByRole('option', { name: /Woolworths/ });
    expect(api.get).toHaveBeenCalledTimes(1);
    expect(api.get).toHaveBeenCalledWith('/api/v1/donors', { params: { search: 'wool', pageSize: 5 } });
  });

  it('navigates to the donor when a result is clicked', async () => {
    renderAs(['Admin']);
    type('wool');
    fireEvent.click(await screen.findByRole('option', { name: /Woolworths/ }));
    expect(location()).toBe(`/donors/${DONOR_ID}`);
  });

  it('"See all" opens the donor list with the search applied', async () => {
    renderAs(['Admin']);
    type('wool & co');
    fireEvent.click(await screen.findByRole('option', { name: /See all donor results/ }));
    expect(location()).toBe('/donors?search=wool%20%26%20co');
  });

  it('supports arrow keys and Enter', async () => {
    renderAs(['Admin']);
    type('wool');
    await screen.findByRole('option', { name: /Woolworths/ });
    fireEvent.keyDown(input(), { key: 'ArrowDown' });
    expect(screen.getByRole('option', { name: /See all donor results/ })).toHaveAttribute('aria-selected', 'true');
    fireEvent.keyDown(input(), { key: 'Enter' });
    expect(location()).toBe('/donors?search=wool');
  });

  it('shows Marketing page results only and never requests donors', async () => {
    renderAs(['Marketing']);
    type('do');
    await new Promise((r) => setTimeout(r, 400));
    expect(api.get).not.toHaveBeenCalled();
    expect(screen.getByRole('option', { name: /Donors/ })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: /See all/ })).not.toBeInTheDocument();
  });

  it('hides Admin-only pages from non-Admins', () => {
    renderAs(['Procurement']);
    expect(screen.getByRole('option', { name: /Tasks/ })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: /Reports/ })).not.toBeInTheDocument();
  });

  it('still lists pages when the donor search fails', async () => {
    vi.mocked(api.get).mockRejectedValue(new Error('boom'));
    renderAs(['Admin']);
    type('re');
    await waitFor(() => expect(screen.getByText(/Couldn't search donors/)).toBeInTheDocument());
    expect(screen.getByRole('option', { name: /Reports/ })).toBeInTheDocument();
  });
});
