import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { ListPagination } from './ListPagination';

afterEach(cleanup);

describe('ListPagination', () => {
  it('summarises the range with the given item label and numbers each page', async () => {
    const onPageChange = vi.fn();
    render(
      <ListPagination
        pagination={{ page: 2, pageSize: 20, totalCount: 54, totalPages: 3 }}
        onPageChange={onPageChange}
        onPageSizeChange={vi.fn()}
        itemLabel="tasks"
      />,
    );

    expect(screen.getByText('Showing 21–40 of 54 tasks')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: '3' }));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it('switches to "Page x of y" past seven pages and reports page-size changes', async () => {
    const onPageSizeChange = vi.fn();
    render(
      <ListPagination
        pagination={{ page: 1, pageSize: 10, totalCount: 95, totalPages: 10 }}
        onPageChange={vi.fn()}
        onPageSizeChange={onPageSizeChange}
        itemLabel="approvals"
      />,
    );

    expect(screen.getByText('Page 1 of 10')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled();
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Rows per page' }), '50');
    expect(onPageSizeChange).toHaveBeenCalledWith(50);
  });
});
