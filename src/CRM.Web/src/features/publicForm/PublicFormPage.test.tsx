// @vitest-environment jsdom
import { describe, it, expect, afterEach } from 'vitest';
import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import PublicFormPage from './PublicFormPage';
import { paths } from '@/routes/paths';

function renderForm() {
  return render(
    <MemoryRouter initialEntries={[paths.publicDonorForm]}>
      <Routes>
        <Route path={paths.publicDonorForm} element={<PublicFormPage />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('PublicFormPage', () => {
  afterEach(cleanup);

  it('renders with zero authentication and no app-shell chrome', () => {
    renderForm();

    expect(screen.getByText('S.A. Harvest')).toBeInTheDocument();
    expect(screen.getByText(/step 1 of 7/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /log out/i })).not.toBeInTheDocument();
  });

  it('does not show a Back button on the first step', () => {
    renderForm();

    expect(screen.queryByRole('button', { name: /back/i })).not.toBeInTheDocument();
  });

  it('advances to the next step and shows Back once past the first step', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /^next$/i }));

    expect(screen.getByText(/step 2 of 7/i)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Contacts' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
  });

  it('goes back to the previous step', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /^next$/i }));
    await user.click(screen.getByRole('button', { name: /back/i }));

    expect(screen.getByText(/step 1 of 7/i)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Company Information' })).toBeInTheDocument();
  });

  it('shows Submit on the final step', async () => {
    const user = userEvent.setup();
    renderForm();

    for (let i = 0; i < 6; i++) {
      await user.click(screen.getByRole('button', { name: /^next$/i }));
    }

    expect(screen.getByText(/step 7 of 7/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^submit$/i })).toBeInTheDocument();
  });
});
