import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TaskList } from './TaskList';
import type { TaskDto } from '../types';

vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));

function task(overrides: Partial<TaskDto> = {}): TaskDto {
  return {
    id: 'task-1',
    title: 'Send vehicle availability',
    description: 'Dineo is waiting on confirmation.',
    dueDate: '2020-01-01T00:00:00',
    isCompleted: false,
    completedAt: null,
    createdAt: '2026-09-01T09:00:00Z',
    donor: { id: 'donor-1', companyName: 'FoodCorp SA' },
    assignedTo: { id: 'user-1', fullName: 'Nomsa Khumalo' },
    createdBy: { id: 'user-2', fullName: 'Keegan Roux' },
    completedBy: null,
    ...overrides,
  };
}

function renderList(ui: React.ReactElement) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TaskList', () => {
  afterEach(cleanup);

  it('renders a row per task with title, donor and an overdue marker', () => {
    renderList(
      <TaskList tasks={[task()]} isPending={false} isError={false} onRetry={() => {}} onEditTask={() => {}} />,
    );
    expect(screen.getByText('Send vehicle availability')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'FoodCorp SA' })).toBeInTheDocument();
    expect(screen.getByText(/overdue/i)).toBeInTheDocument();
  });

  it('shows the empty state', () => {
    renderList(
      <TaskList
        tasks={[]}
        isPending={false}
        isError={false}
        onRetry={() => {}}
        onEditTask={() => {}}
        emptyTitle="No open tasks"
      />,
    );
    expect(screen.getByText('No open tasks')).toBeInTheDocument();
  });

  it('renders a completed task struck through and without the donor chip when scoped', () => {
    renderList(
      <TaskList
        tasks={[task({ isCompleted: true, completedAt: '2026-09-02T00:00:00' })]}
        isPending={false}
        isError={false}
        onRetry={() => {}}
        onEditTask={() => {}}
        showDonor={false}
      />,
    );
    expect(screen.queryByRole('link', { name: 'FoodCorp SA' })).not.toBeInTheDocument();
    expect(screen.getByText(/Completed/)).toBeInTheDocument();
  });
});
