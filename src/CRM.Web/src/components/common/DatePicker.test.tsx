import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { DatePicker } from './DatePicker';

afterEach(cleanup);

describe('DatePicker', () => {
  it('shows the placeholder, opens the calendar and emits the picked day as yyyy-MM-dd', async () => {
    const onChange = vi.fn();
    render(<DatePicker value="2026-10-02" onChange={onChange} aria-label="Due date" />);

    expect(screen.getByRole('button', { name: 'Due date' })).toHaveTextContent('02 Oct 2026');
    await userEvent.click(screen.getByRole('button', { name: 'Due date' }));
    await userEvent.click(await screen.findByRole('button', { name: /October 15th, 2026/i }));

    expect(onChange).toHaveBeenCalledWith('2026-10-15');
  });

  it('disables days before min', async () => {
    render(<DatePicker value="2026-10-10" min="2026-10-05" onChange={vi.fn()} aria-label="Due date" />);
    await userEvent.click(screen.getByRole('button', { name: 'Due date' }));

    expect(await screen.findByRole('button', { name: /October 4th, 2026/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /October 5th, 2026/i })).toBeEnabled();
  });

  it('offers "Clear date" only when clearable and set', async () => {
    const onChange = vi.fn();
    render(<DatePicker value="2026-10-10" onChange={onChange} clearable aria-label="Follow-up" />);
    await userEvent.click(screen.getByRole('button', { name: 'Follow-up' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Clear date' }));

    expect(onChange).toHaveBeenCalledWith('');
  });

  it('shows the placeholder when empty', () => {
    render(<DatePicker value="" onChange={vi.fn()} placeholder="No follow-up date" aria-label="Follow-up" />);
    expect(screen.getByRole('button', { name: 'Follow-up' })).toHaveTextContent('No follow-up date');
  });
});
