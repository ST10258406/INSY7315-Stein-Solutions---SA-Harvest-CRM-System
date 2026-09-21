// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { ConfirmationScreen } from './ConfirmationScreen';

function renderScreen() {
  return render(
    <MemoryRouter>
      <ConfirmationScreen message="Thank you. Your submission has been received and is currently under review." referenceNumber="DON-2026-00042" />
    </MemoryRouter>
  );
}

describe('ConfirmationScreen', () => {
  afterEach(cleanup);

  it('displays the backend message and reference number', () => {
    renderScreen();

    expect(screen.getByText(/your submission has been received/i)).toBeInTheDocument();
    expect(screen.getByText('DON-2026-00042')).toBeInTheDocument();
  });

  it('copies the reference number to the clipboard', async () => {
    // userEvent.setup() installs its own clipboard stub on `navigator` — spy
    // on that stub's writeText rather than replacing the whole object,
    // which setup() would otherwise clobber.
    const user = userEvent.setup();
    const writeText = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue(undefined);
    renderScreen();

    await user.click(screen.getByRole('button', { name: /copy reference number/i }));

    expect(writeText).toHaveBeenCalledWith('DON-2026-00042');
    expect(await screen.findByText(/copied to clipboard/i)).toBeInTheDocument();
  });

  it('does not show a back button — only a forward "return to homepage" action', () => {
    renderScreen();

    expect(screen.queryByRole('button', { name: /back/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /return to homepage/i })).toBeInTheDocument();
  });
});
