// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, cleanup, within } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { toast } from 'sonner';
import PublicFormPage from './PublicFormPage';
import { paths } from '@/routes/paths';

vi.mock('sonner', () => ({
  toast: { error: vi.fn(), info: vi.fn() },
}));

const lookupData = [{ id: 1, name: 'Option A' }];

vi.mock('./hooks/usePublicLookups', () => ({
  usePublicCompanyTypes: () => ({ data: lookupData, isPending: false }),
  usePublicEntityTypes: () => ({ data: lookupData, isPending: false }),
  usePublicOperationalRegions: () => ({ data: lookupData, isPending: false }),
  usePublicDonationTypes: () => ({ data: lookupData, isPending: false }),
  usePublicDonationFrequencies: () => ({ data: lookupData, isPending: false }),
  usePublicProvinces: () => ({ data: lookupData, isPending: false }),
  usePublicBbbeeStatuses: () => ({ data: lookupData, isPending: false }),
}));

// The real canvas-drawing behaviour is covered by SignaturePad.test.tsx —
// here it's swapped for a stub so step-gating logic (must sign before Next
// works) can be tested without jsdom's lack of a real <canvas> context.
vi.mock('./components/SignaturePad', () => ({
  SignaturePad: ({ onChange, error }: { onChange: (dataUrl: string | null) => void; error?: string }) => (
    <div>
      <button type="button" onClick={() => onChange('data:image/png;base64,MOCK')}>
        Simulate sign
      </button>
      {error && <span>{error}</span>}
    </div>
  ),
}));

const mutateAsync = vi.fn();
vi.mock('./hooks/useSubmitPublicDonor', () => ({
  useSubmitPublicDonor: () => ({ mutateAsync, isPending: false }),
}));

// The actual document upload (file picking, retry, session-expiry handling)
// is covered separately — here it's a stub so the page-level phase
// transition (form -> document upload -> complete) can be tested in
// isolation.
vi.mock('./components/DocumentUploadStep', () => ({
  DocumentUploadStep: ({ submissionToken, onUploaded }: { submissionToken: string; onUploaded: () => void }) => (
    <div>
      <span>Uploading for token: {submissionToken}</span>
      <button type="button" onClick={onUploaded}>
        Simulate upload success
      </button>
    </div>
  ),
}));

function renderForm() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[paths.publicDonorForm]}>
        <Routes>
          <Route path={paths.publicDonorForm} element={<PublicFormPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

async function fillCompanyStep(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByRole('textbox', { name: /^company name/i }), 'Fresh Fields Wholesale');
  await user.selectOptions(screen.getByRole('combobox', { name: /company type/i }), '1');
  await user.type(screen.getByRole('textbox', { name: /website/i }), 'freshfields.co.za');
  await user.type(screen.getByRole('textbox', { name: /registered company name/i }), 'Fresh Fields Wholesale (Pty) Ltd');
  await user.type(screen.getByRole('textbox', { name: /trading name/i }), 'Fresh Fields');
  await user.selectOptions(screen.getByRole('combobox', { name: /legal entity type/i }), '1');
  await user.type(screen.getByRole('textbox', { name: /company registration number/i }), '2014/183920/07');
  await user.type(screen.getByRole('textbox', { name: /income tax number/i }), '9012345678');
}

async function fillContactsStep(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByRole('textbox', { name: /^name/i }), 'Naledi Khumalo');
  await user.type(screen.getByRole('textbox', { name: /^phone/i }), '+27 82 447 1123');
  await user.type(screen.getByRole('textbox', { name: /^email/i }), 'naledi@freshfields.co.za');
}

async function fillAddressStep(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByRole('textbox', { name: /street name/i }), '14 Loop Street');
  await user.type(screen.getByRole('textbox', { name: /suburb/i }), 'Bo-Kaap');
  await user.type(screen.getByRole('textbox', { name: /^city/i }), 'Cape Town');
  await user.selectOptions(screen.getByRole('combobox', { name: /province/i }), '1');
  await user.type(screen.getByRole('textbox', { name: /postal code/i }), '8001');
}

async function fillDonationsStep(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByRole('textbox', { name: /collection.*pickup address/i }), 'Warehouse 3, 14 Loop Street');
  for (const chip of screen.getAllByRole('button', { name: 'Option A' })) {
    await user.click(chip);
  }
  await user.selectOptions(screen.getByRole('combobox', { name: /donation frequency/i }), '1');
}

async function completeFullFormUpToReview(user: ReturnType<typeof userEvent.setup>) {
  await fillCompanyStep(user);
  await user.click(screen.getByRole('button', { name: /^next$/i }));
  await screen.findByRole('heading', { name: 'Contacts' });

  await fillContactsStep(user);
  await user.click(screen.getByRole('button', { name: /^next$/i }));
  await screen.findByRole('heading', { name: 'Registered Address' });

  await fillAddressStep(user);
  await user.click(screen.getByRole('button', { name: /^next$/i }));
  await screen.findByRole('heading', { name: 'Donation Information' });

  await fillDonationsStep(user);
  await user.click(screen.getByRole('button', { name: /^next$/i }));
  await screen.findByRole('heading', { name: 'Compliance' });

  await user.click(screen.getByRole('button', { name: /^next$/i }));
  await screen.findByRole('heading', { name: 'Signature' });

  await user.click(screen.getByRole('button', { name: /simulate sign/i }));
  await user.click(screen.getByRole('button', { name: /^next$/i }));
  await screen.findByRole('heading', { name: 'Review' });
}

describe('PublicFormPage', () => {
  afterEach(() => {
    cleanup();
    mutateAsync.mockReset();
    vi.mocked(toast.error).mockReset();
    vi.mocked(toast.info).mockReset();
  });

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

  it('blocks advancing past step 1 when required fields are missing', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /^next$/i }));

    expect(screen.getByText(/step 1 of 7/i)).toBeInTheDocument();
    expect(await screen.findByText(/^company name is required/i)).toBeInTheDocument();
  });

  it('advances once step 1 is filled in validly', async () => {
    const user = userEvent.setup();
    renderForm();

    await fillCompanyStep(user);
    await user.click(screen.getByRole('button', { name: /^next$/i }));

    expect(await screen.findByText(/step 2 of 7/i)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Contacts' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
  });

  it('goes back to the previous step and keeps entered data', async () => {
    const user = userEvent.setup();
    renderForm();

    await fillCompanyStep(user);
    await user.click(screen.getByRole('button', { name: /^next$/i }));
    await user.click(screen.getByRole('button', { name: /back/i }));

    expect(await screen.findByText(/step 1 of 7/i)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Company Information' })).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: /^company name/i })).toHaveValue('Fresh Fields Wholesale');
  });

  it('only requires marketing/accounts contact fields once their section is toggled on', async () => {
    const user = userEvent.setup();
    renderForm();

    await fillCompanyStep(user);
    await user.click(screen.getByRole('button', { name: /^next$/i }));
    await screen.findByRole('heading', { name: 'Contacts' });

    await user.type(screen.getByRole('textbox', { name: /^name/i }), 'Naledi Khumalo');
    await user.type(screen.getByRole('textbox', { name: /^phone/i }), '+27 82 447 1123');
    await user.type(screen.getByRole('textbox', { name: /^email/i }), 'naledi@freshfields.co.za');

    // Next succeeds without marketing/accounts details filled in, since neither is toggled on.
    await user.click(screen.getByRole('button', { name: /^next$/i }));
    expect(await screen.findByText(/step 3 of 7/i)).toBeInTheDocument();
  });

  it('jumps directly to a step via the step indicator, matching the approved design', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /step 7: review/i }));

    expect(await screen.findByText(/step 7 of 7/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^submit$/i })).toBeInTheDocument();
  });

  it('requires the confirmation checkbox before submitting on the review step', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /step 7: review/i }));
    await user.click(screen.getByRole('button', { name: /^submit$/i }));

    expect(await screen.findByText(/please confirm before submitting/i)).toBeInTheDocument();
  });

  it('shows the review step\'s Edit link for each section and jumps back to it', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /step 7: review/i }));
    const companyGroup = screen.getByText('Company Information').closest('div');
    expect(companyGroup).not.toBeNull();

    await user.click(within(companyGroup!).getByRole('button', { name: /edit/i }));

    expect(await screen.findByText(/step 1 of 7/i)).toBeInTheDocument();
  });

  it('blocks advancing off the signature step until a signature is captured', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /step 6: signature/i }));
    await user.click(screen.getByRole('button', { name: /^next$/i }));

    expect(await screen.findByText(/a signature is required/i)).toBeInTheDocument();
    expect(screen.getByText(/step 6 of 7/i)).toBeInTheDocument();
  });

  it('advances off the signature step once a signature is captured', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /step 6: signature/i }));
    await user.click(screen.getByRole('button', { name: /simulate sign/i }));
    await user.click(screen.getByRole('button', { name: /^next$/i }));

    expect(await screen.findByText(/step 7 of 7/i)).toBeInTheDocument();
  });

  it('reflects the captured signature on the review step', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('button', { name: /step 6: signature/i }));
    await user.click(screen.getByRole('button', { name: /simulate sign/i }));
    await user.click(screen.getByRole('button', { name: /step 7: review/i }));

    expect(await screen.findByText(/yes — signature captured/i)).toBeInTheDocument();
  });

  it('submits the full form and moves to the document upload phase on success', async () => {
    mutateAsync.mockResolvedValue({
      message: 'Thank you.',
      referenceNumber: 'DON-2026-00042',
      submissionToken: 'tok-abc123',
    });
    const user = userEvent.setup();
    renderForm();

    await completeFullFormUpToReview(user);
    await user.click(screen.getByRole('checkbox', { name: /confirm this information is accurate/i }));
    await user.click(screen.getByRole('button', { name: /^submit$/i }));

    expect(await screen.findByText(/uploading for token: tok-abc123/i)).toBeInTheDocument();
    expect(mutateAsync).toHaveBeenCalledTimes(1);
  }, 15000);

  it('shows an error toast and stays on the review step when submission fails', async () => {
    mutateAsync.mockRejectedValue({ response: { data: { message: 'Server exploded.' } } });
    const user = userEvent.setup();
    renderForm();

    await completeFullFormUpToReview(user);
    await user.click(screen.getByRole('checkbox', { name: /confirm this information is accurate/i }));
    await user.click(screen.getByRole('button', { name: /^submit$/i }));

    await vi.waitFor(() => expect(toast.error).toHaveBeenCalledWith('Server exploded.'));
    expect(screen.getByText(/step 7 of 7/i)).toBeInTheDocument();
  }, 15000);

  it('shows the confirmation placeholder once the document upload succeeds', async () => {
    mutateAsync.mockResolvedValue({
      message: 'Thank you. Your submission has been received.',
      referenceNumber: 'DON-2026-00042',
      submissionToken: 'tok-abc123',
    });
    const user = userEvent.setup();
    renderForm();

    await completeFullFormUpToReview(user);
    await user.click(screen.getByRole('checkbox', { name: /confirm this information is accurate/i }));
    await user.click(screen.getByRole('button', { name: /^submit$/i }));

    await user.click(await screen.findByRole('button', { name: /simulate upload success/i }));

    expect(await screen.findByText(/DON-2026-00042/)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /thank you/i })).toBeInTheDocument();
  }, 15000);
});
