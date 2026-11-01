import { render, screen, cleanup, fireEvent, waitFor, configure } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { api } from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { CalendarDialog } from './CalendarDialog';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn() } }));

// The first render (DayPicker + two queries) can exceed the 1 s default when the whole suite runs in parallel.
configure({ asyncUtilTimeout: 5000 });

const USER_ID = 'user-1';
const DONOR_ID = 'donor-1';

const page = <T,>(data: T[], totalCount = data.length) => ({
  data: { data, pagination: { page: 1, pageSize: 100, totalCount, totalPages: 1 } },
});

const task = (id: string, dueDate: string, title: string) => ({
  id,
  title,
  dueDate,
  isCompleted: false,
  donor: { id: DONOR_ID, companyName: 'Woolworths' },
});

function LocationProbe() {
  return <div data-testid="location">{useLocation().pathname}</div>;
}

function mockApi(tasks: unknown[], donors: unknown[] = [], taskTotal?: number) {
  vi.mocked(api.get).mockImplementation((url: string) =>
    Promise.resolve(url === '/api/v1/tasks' ? page(tasks, taskTotal) : page(donors)),
  );
}

function renderAs(roles: string[]) {
  useAuthStore.setState({ user: { id: USER_ID, firstName: 'A', lastName: 'B', email: 'a@b.c', roles } } as Partial<
    ReturnType<typeof useAuthStore.getState>
  >);
  render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <CalendarDialog open onOpenChange={() => {}} />
        <LocationProbe />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

const calls = (url: string) => vi.mocked(api.get).mock.calls.filter(([u]) => u === url);

describe('CalendarDialog', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-10-20T08:00:00Z')); // 10:00 SAST, 20 October
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.mocked(api.get).mockReset();
  });

  it('requests the month, padded a day each side, for open tasks and the caller\'s donors', async () => {
    mockApi([]);
    renderAs(['Procurement']);
    await screen.findByText('Nothing due this month.');

    expect(calls('/api/v1/tasks')[0][1]).toEqual({
      params: { isCompleted: false, dueAfter: '2026-09-30', dueBefore: '2026-11-01', pageSize: 100 },
    });
    expect(calls('/api/v1/donors')[0][1]).toEqual({
      params: { relationshipManagerId: USER_ID, followUpAfter: '2026-09-30', followUpBefore: '2026-11-01', pageSize: 100 },
    });
  });

  it('groups by SAST day: 00:30 SAST on the 15th is not shown on the 14th', async () => {
    mockApi([task('t1', '2026-10-14T22:30:00Z', 'Call back')]);
    renderAs(['Admin']);

    fireEvent.click(await screen.findByRole('button', { name: /October 15th.*1 item/ }));
    expect(screen.getByRole('button', { name: /Call back/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /October 14th.*item/ })).not.toBeInTheDocument();
  });

  it('marks past days with open items as overdue', async () => {
    mockApi([task('t1', '2026-10-12T08:00:00Z', 'Send quote')]);
    renderAs(['Admin']);

    fireEvent.click(await screen.findByRole('button', { name: /October 12th.*1 item, 1 overdue/ }));
    expect(screen.getByRole('button', { name: /Send quote.*Overdue/ })).toBeInTheDocument();
  });

  it('shows donor follow-ups and opens the donor when clicked', async () => {
    mockApi([], [{ id: DONOR_ID, companyName: 'Pick n Pay', status: 'Active', followUpDate: '2026-10-20T07:00:00Z' }]);
    renderAs(['Procurement']);

    fireEvent.click(await screen.findByRole('button', { name: /Pick n Pay/ }));
    expect(screen.getByTestId('location')).toHaveTextContent(`/donors/${DONOR_ID}`);
  });
  it('opens a new task due on the double-clicked day, but not for past days', async () => {
    mockApi([]);
    renderAs(['Procurement']);
    await screen.findByText('Nothing due this month.');

    // Browsers send a click before the dblclick; the click is what selects the day.
    const doubleClick = (el: HTMLElement) => {
      fireEvent.click(el);
      fireEvent.doubleClick(el);
    };
    doubleClick(screen.getByRole('button', { name: /October 19th/ }));
    expect(screen.queryByRole('heading', { name: 'New task' })).not.toBeInTheDocument();

    doubleClick(screen.getByRole('button', { name: /October 22nd/ }));
    expect(await screen.findByRole('heading', { name: 'New task' })).toBeInTheDocument();
    expect(screen.getByLabelText(/Due date/)).toHaveTextContent(/22/);
  });

  it('never requests donors for Marketing', async () => {
    mockApi([]);
    renderAs(['Marketing']);
    await screen.findByText('Nothing due this month.');
    expect(calls('/api/v1/donors')).toHaveLength(0);
  });

  it('refetches when moving to the next month', async () => {
    mockApi([]);
    renderAs(['Marketing']);
    await screen.findByText('Nothing due this month.');

    fireEvent.click(screen.getByRole('button', { name: /next month/i }));
    await waitFor(() =>
      expect(calls('/api/v1/tasks').at(-1)![1]).toEqual({
        params: { isCompleted: false, dueAfter: '2026-10-31', dueBefore: '2026-12-01', pageSize: 100 },
      }),
    );
  });

  it('jumps back to today from another month', async () => {
    mockApi([]);
    renderAs(['Marketing']);
    await screen.findByText('Nothing due this month.');
    const todayButton = screen.getByRole('button', { name: 'Today' });
    expect(todayButton).toBeDisabled();

    fireEvent.click(screen.getByRole('button', { name: /next month/i }));
    fireEvent.click(todayButton);
    await waitFor(() => expect(calls('/api/v1/tasks').at(-1)![1]).toMatchObject({ params: { dueAfter: '2026-09-30' } }));
    expect(screen.getByRole('heading', { name: /Today · .*20 October/ })).toBeInTheDocument();
  });

  it('says when the month has more items than were loaded', async () => {
    mockApi([task('t1', '2026-10-21T08:00:00Z', 'One')], [], 140);
    renderAs(['Marketing']);
    expect(await screen.findByText('Showing the first 100 items.')).toBeInTheDocument();
  });
});
