import { render, screen, cleanup, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { api } from '@/lib/axios';
import { VettingForm } from './VettingForm';
import { VETTING_SECTIONS } from '../lib/vettingSections';
import { validFormValues } from '../schemas/vettingFormFixtures';

vi.mock('@/lib/axios', () => ({ api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }));

const donorDetail = {
  id: 'd1',
  company: { companyName: 'FoodCorp SA', registeredCompanyName: 'FoodCorp South Africa (Pty) Ltd' },
  legalAddress: { province: { id: 3, code: 'GP', name: 'Gauteng' } },
};

function renderForm(prefill = {}) {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <VettingForm mode="create" prefill={prefill} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const header = (title: string) => screen.getByRole('button', { name: new RegExp(title.replace(/[&]/g, '\\$&')) });
const isOpen = (title: string) => header(title).getAttribute('aria-expanded') === 'true';

describe('VettingForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.get).mockImplementation(async (url: string) => {
      if (url === '/api/v1/vetting/officers') return { data: { data: [{ id: 'o1', fullName: 'Thandi Mokoena', initials: 'TM' }] } };
      if (url === '/api/v1/donors')
        return { data: { data: [{ id: 'd1', companyName: 'FoodCorp SA' }], pagination: { page: 1, pageSize: 8, totalCount: 1, totalPages: 1 } } };
      if (url === '/api/v1/donors/d1') return { data: { data: donorDetail } };
      throw new Error(`unexpected GET ${url}`);
    });
  });
  afterEach(cleanup);

  it('opens only the sections with errors on an empty submit and shows their badges', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(screen.getByRole('button', { name: 'Collapse all' }));
    await user.click(screen.getByRole('button', { name: 'Save vetting' }));

    await waitFor(() => expect(isOpen('Organisation & Contact')).toBe(true));
    for (const title of ['Location', 'Programmes & Services', 'Registration', 'Demographics', 'Feeding Operation', 'Facilities & Hygiene', 'Access & Security', 'Documents'])
      expect(isOpen(title)).toBe(true);
    // Staffing counts default to 0 and capacity notes are optional: nothing to fix there.
    for (const title of ['Staffing', 'Capacity Notes', 'Evidence Photos']) expect(isOpen(title)).toBe(false);

    expect(header('Registration')).toHaveTextContent('2 errors');
    expect(header('Documents')).toHaveTextContent('3 errors');
    expect(api.post).not.toHaveBeenCalled();
  });

  it('keeps a typed value when its section is collapsed and reopened', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(header('Location'));
    await user.type(screen.getByLabelText('What3Words'), '///apple.orange.banana');

    await user.click(header('Location'));
    expect(isOpen('Location')).toBe(false);
    await user.click(header('Location'));

    expect(screen.getByLabelText('What3Words')).toHaveValue('///apple.orange.banana');
  });

  it('Expand all opens all 12 sections', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(screen.getByRole('button', { name: 'Expand all' }));
    expect(VETTING_SECTIONS).toHaveLength(12);
    for (const section of VETTING_SECTIONS) expect(isOpen(section.title)).toBe(true);
  });

  it('pre-fills the legal name and province when a donor is chosen', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(screen.getByRole('button', { name: /search donors/i }));
    await user.click(await screen.findByRole('button', { name: 'FoodCorp SA' }));

    await waitFor(() => expect(screen.getByLabelText('Organisation legal name')).toHaveValue('FoodCorp South Africa (Pty) Ltd'));
    expect(screen.getByLabelText('Province')).toHaveValue('Gauteng');
  });

  it('shows the donor read-only when it comes from the query string', async () => {
    renderForm({ donorId: 'd1', officerId: 'o1' });
    expect(await screen.findByText('FoodCorp SA')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /search donors/i })).not.toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText('Officer')).toHaveValue('o1'));
  });

  it('turns the subtotal hint amber when the subtotals do not match the total', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(header('Feeding Operation'));
    const total = screen.getByLabelText('Total people served');
    await user.clear(total);
    await user.type(total, '50');
    const females = screen.getByLabelText('Females served');
    await user.clear(females);
    await user.type(females, '40');

    const hint = screen.getByText('Gender subtotal 40 of 50');
    expect(hint).toHaveAttribute('data-mismatch', 'true');
    expect(hint.className).toContain('amber');

    const males = screen.getByLabelText('Males served');
    await user.clear(males);
    await user.type(males, '10');
    expect(screen.getByText('Gender subtotal 50 of 50')).toHaveAttribute('data-mismatch', 'false');
  });
});

describe('VettingForm — edit mode', () => {
  afterEach(cleanup);

  function renderEdit(approvalState: string, decisionReason: string | null = null) {
    const details = { ...validFormValues().details } as Record<string, unknown>;
    for (const key of ['isRegisteredNpo', 'isRegisteredDsd', 'kitchenClean', 'waterAccess', 'toiletFacilities', 'pestFree',
      'deliveryAccessEase', 'parkingAndSecurity', 'slaSigned', 'consentFormSigned', 'policyDocumentPresent'])
      details[key] = details[key] === 'true';
    vi.mocked(api.get).mockImplementation(async (url: string) => {
      if (url === '/api/v1/vetting/officers') return { data: { data: [{ id: 'o1', fullName: 'Thandi Mokoena', initials: 'TM' }] } };
      if (url === '/api/v1/vetting/submissions/s1')
        return {
          data: {
            data: {
              id: 's1', formReference: 'VET-2026-0007', donorId: 'd1', donorCompanyName: 'FoodCorp SA', officerId: 'o1',
              officerFullName: 'Thandi Mokoena', siteVisitId: null, approvalState, decisionReason, details,
              flags: [], photos: [], files: {},
            },
          },
        };
      throw new Error(`unexpected GET ${url}`);
    });
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <MemoryRouter>
          <VettingForm mode="edit" submissionId="s1" />
        </MemoryRouter>
      </QueryClientProvider>,
    );
  }

  it('shows a read-only notice and no Save button once submitted', async () => {
    renderEdit('Ready');
    expect(await screen.findByText(/can no longer be edited/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument();
  });

  it('shows the admin’s reason when returned, and a server 400 opens the section it points at', async () => {
    const user = userEvent.setup();
    vi.mocked(api.patch).mockRejectedValue({
      response: { status: 400, data: { code: 'VALIDATION_ERROR', message: 'x', errors: [{ field: 'Details.ContactEmail', message: 'Rejected by the server' }] } },
    });
    renderEdit('Returned', 'Toilets need re-inspection');

    expect(await screen.findByRole('alert')).toHaveTextContent('Toilets need re-inspection');
    await waitFor(() => expect(screen.getByLabelText('Organisation legal name')).toHaveValue('Ubuntu Food Co (Pty) Ltd'));
    expect(isOpen('Organisation & Contact')).toBe(false); // edit mode starts collapsed

    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(isOpen('Organisation & Contact')).toBe(true));
    expect(screen.getByText('Rejected by the server')).toBeInTheDocument();
    expect(header('Organisation & Contact')).toHaveTextContent('1 error');
  });
});
