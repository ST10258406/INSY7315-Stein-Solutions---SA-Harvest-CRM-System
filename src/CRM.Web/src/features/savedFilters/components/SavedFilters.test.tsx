import { render, screen, cleanup, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { api } from '@/lib/axios';
import { SavedFiltersMenu } from './SavedFiltersMenu';
import { SaveFilterDialog } from './SaveFilterDialog';
import { filterQueryString } from '../schemas/saveFilterSchema';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), post: vi.fn(), delete: vi.fn() } }));

function LocationProbe() {
  const { pathname, search } = useLocation();
  return <div data-testid="location">{pathname + search}</div>;
}

function renderWithin(ui: React.ReactNode, route = '/dashboard') {
  render(
    <MemoryRouter initialEntries={[route]}>
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })}>
        {ui}
        <LocationProbe />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

const FILTER = { id: 'f1', name: 'Gauteng active', page: 'Donors', queryString: 'status=Active&regionCode=GP' };

afterEach(() => {
  cleanup();
  vi.mocked(api.get).mockReset();
  vi.mocked(api.post).mockReset();
  vi.mocked(api.delete).mockReset();
});

describe('filterQueryString', () => {
  it('drops paging but keeps filters and sort', () => {
    expect(filterQueryString('?status=Active&page=3&pageSize=50&sortBy=companyName')).toBe('status=Active&sortBy=companyName');
  });
});

describe('SavedFiltersMenu', () => {
  const open = () => fireEvent.click(screen.getByRole('button', { name: 'Saved filters' }));

  it('fetches only once opened, then applies a filter by navigating to the donor list', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: [FILTER] });
    renderWithin(<SavedFiltersMenu triggerClassName="" />);
    expect(api.get).not.toHaveBeenCalled();

    open();
    fireEvent.click(await screen.findByRole('button', { name: 'Gauteng active' }));

    expect(screen.getByTestId('location')).toHaveTextContent('/donors?status=Active&regionCode=GP');
  });

  it('asks for confirmation before deleting, then deletes', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: [FILTER] });
    vi.mocked(api.delete).mockResolvedValue({});
    renderWithin(<SavedFiltersMenu triggerClassName="" />);
    open();

    fireEvent.click(await screen.findByRole('button', { name: 'Delete saved filter Gauteng active' }));
    expect(api.delete).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));

    await waitFor(() => expect(api.delete).toHaveBeenCalledWith('/api/v1/saved-filters/f1'));
    await waitFor(() => expect(api.get).toHaveBeenCalledTimes(2)); // list invalidated and refetched
  });

  it('shows the empty state', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: [] });
    renderWithin(<SavedFiltersMenu triggerClassName="" />);
    open();
    expect(await screen.findByText(/No saved filters yet/)).toBeInTheDocument();
  });
});

describe('SaveFilterDialog', () => {
  const nameInput = () => screen.getByLabelText('Name');
  const submit = () => fireEvent.click(screen.getByRole('button', { name: 'Save' }));

  it('saves the current URL filters without paging', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: { data: FILTER } });
    const onOpenChange = vi.fn();
    renderWithin(<SaveFilterDialog open onOpenChange={onOpenChange} />, '/donors?status=Active&regionCode=GP&page=2');

    fireEvent.change(nameInput(), { target: { value: '  Gauteng active  ' } });
    submit();

    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/api/v1/saved-filters', {
        name: 'Gauteng active',
        page: 'Donors',
        queryString: 'status=Active&regionCode=GP',
      }),
    );
    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false));
  });

  it('requires a name', async () => {
    renderWithin(<SaveFilterDialog open onOpenChange={() => {}} />, '/donors?status=Active');
    submit();
    expect(await screen.findByText('Enter a name')).toBeInTheDocument();
    expect(api.post).not.toHaveBeenCalled();
  });

  it('shows a duplicate-name error on 409', async () => {
    vi.mocked(api.post).mockRejectedValue({ response: { status: 409, data: { status: 409, code: 'CONFLICT', message: 'dup' } } });
    renderWithin(<SaveFilterDialog open onOpenChange={() => {}} />, '/donors?status=Active');

    fireEvent.change(nameInput(), { target: { value: 'Gauteng active' } });
    submit();

    expect(await screen.findByText('You already have a saved filter with this name.')).toBeInTheDocument();
  });
});
