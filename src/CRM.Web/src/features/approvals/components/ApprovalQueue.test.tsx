import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ApprovalQueue } from './ApprovalQueue';
import type { ApprovalDto } from '../types';

vi.mock('@/lib/axios', () => ({ api: { post: vi.fn() } }));

function approval(overrides: Partial<ApprovalDto> = {}): ApprovalDto {
  return {
    id: 'appr-1',
    status: 'Pending',
    rejectionReason: null,
    reviewedAt: null,
    createdAt: '2026-09-01T09:00:00Z',
    donor: { id: 'donor-1', companyName: 'FoodCorp SA', status: 'PendingReview' },
    requestedBy: { id: 'user-1', fullName: 'Riaan Botha' },
    reviewedBy: null,
    ...overrides,
  };
}

function renderQueue(ui: React.ReactElement) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ApprovalQueue', () => {
  afterEach(cleanup);

  it('renders a pending row with Approve/Reject and an inline approve confirm', async () => {
    renderQueue(
      <ApprovalQueue
        approvals={[approval()]}
        isPending={false}
        isError={false}
        onRetry={() => {}}
        onReject={() => {}}
        status="Pending"
      />,
    );

    expect(screen.getByRole('link', { name: 'FoodCorp SA' })).toHaveAttribute('href', '/donors/donor-1');
    expect(screen.getByRole('button', { name: /reject/i })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /approve/i }));
    expect(screen.getByText('Approve this donor?')).toBeInTheDocument();
  });

  it('calls onReject with the approval when Reject is clicked', async () => {
    const onReject = vi.fn();
    renderQueue(
      <ApprovalQueue
        approvals={[approval()]}
        isPending={false}
        isError={false}
        onRetry={() => {}}
        onReject={onReject}
        status="Pending"
      />,
    );
    await userEvent.click(screen.getByRole('button', { name: /reject/i }));
    expect(onReject).toHaveBeenCalledWith(expect.objectContaining({ id: 'appr-1' }));
  });

  it('renders a decided row with its outcome and the rejection reason', () => {
    renderQueue(
      <ApprovalQueue
        approvals={[
          approval({
            status: 'Rejected',
            rejectionReason: 'Company registration number does not match CIPC.',
            reviewedAt: '2026-09-02T10:00:00Z',
            reviewedBy: { id: 'user-9', fullName: 'Keegan Roux' },
            donor: { id: 'donor-1', companyName: 'FoodCorp SA', status: 'Rejected' },
          }),
        ]}
        isPending={false}
        isError={false}
        onRetry={() => {}}
        onReject={() => {}}
        status="Rejected"
      />,
    );
    expect(screen.getByText(/Rejected by Keegan Roux/)).toBeInTheDocument();
    expect(screen.getByText('Company registration number does not match CIPC.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /approve/i })).not.toBeInTheDocument();
  });

  it('shows the empty state for the pending tab', () => {
    renderQueue(
      <ApprovalQueue
        approvals={[]}
        isPending={false}
        isError={false}
        onRetry={() => {}}
        onReject={() => {}}
        status="Pending"
      />,
    );
    expect(screen.getByText(/all caught up/i)).toBeInTheDocument();
  });
});
