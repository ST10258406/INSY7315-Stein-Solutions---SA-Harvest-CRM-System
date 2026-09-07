import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { donorKeys } from '@/features/donors/hooks/donorKeys';
import { useMyTasks } from './useMyTasks';
import { useDonorTasks } from './useDonorTasks';
import { useCreateTask } from './useCreateTask';
import { useCompleteTask } from './useCompleteTask';
import { useReopenTask } from './useReopenTask';
import { taskKeys } from './taskKeys';
import type { PaginatedResult, TaskDto } from '../types';

vi.mock('@/lib/axios', () => ({
  api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

const task: TaskDto = {
  id: 'task-1',
  title: 'Confirm collection slot',
  description: null,
  dueDate: '2026-09-10T00:00:00',
  isCompleted: false,
  completedAt: null,
  createdAt: '2026-09-01T09:00:00Z',
  donor: { id: 'donor-1', companyName: 'FoodCorp SA' },
  assignedTo: { id: 'user-1', fullName: 'Nomsa Khumalo' },
  createdBy: { id: 'user-2', fullName: 'Keegan Roux' },
  completedBy: null,
};

const pageOf = (rows: TaskDto[]): PaginatedResult<TaskDto> => ({
  data: rows,
  pagination: { page: 1, pageSize: 20, totalCount: rows.length, totalPages: 1 },
});

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
  };
}

describe('task hooks', () => {
  beforeEach(() => vi.clearAllMocks());

  it('useMyTasks → GETs /api/v1/tasks with filters and returns the paginated result', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: pageOf([task]) });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useMyTasks({ isCompleted: false, page: 1, pageSize: 20 }), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.get).toHaveBeenCalledWith('/api/v1/tasks', { params: { isCompleted: false, page: 1, pageSize: 20 } });
    expect(result.current.data?.data[0].title).toBe('Confirm collection slot');
  });

  it('useDonorTasks → GETs the donor-scoped endpoint and stays idle without an id', async () => {
    const { wrapper } = createWrapper();
    const { result, rerender } = renderHook(({ id }: { id?: string }) => useDonorTasks(id, { isCompleted: 'false' }), {
      wrapper,
      initialProps: {},
    });
    expect(result.current.fetchStatus).toBe('idle');
    expect(api.get).not.toHaveBeenCalled();

    vi.mocked(api.get).mockResolvedValueOnce({ data: pageOf([task]) });
    rerender({ id: 'donor-1' });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.get).toHaveBeenCalledWith('/api/v1/donors/donor-1/tasks', { params: { isCompleted: 'false' } });
  });

  it('useCreateTask → POSTs to the donor tasks endpoint, unwraps { data }, invalidates tasks + donor detail', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: task } });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useCreateTask('donor-1'), { wrapper });

    result.current.mutate({ title: 'New task', assignedToUserId: 'user-1', dueDate: '2026-09-10' });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/donors/donor-1/tasks', {
      title: 'New task',
      assignedToUserId: 'user-1',
      dueDate: '2026-09-10',
    });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: taskKeys.all });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: donorKeys.detail('donor-1') });
  });

  it('useCompleteTask → POSTs /complete and invalidates every task list', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ status: 204 });

    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const { result } = renderHook(() => useCompleteTask(), { wrapper });

    result.current.mutate('task-1');
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/tasks/task-1/complete');
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: taskKeys.all });
  });

  it('useReopenTask → POSTs /reopen', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ status: 204 });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useReopenTask(), { wrapper });

    result.current.mutate('task-1');
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.post).toHaveBeenCalledWith('/api/v1/tasks/task-1/reopen');
  });
});
