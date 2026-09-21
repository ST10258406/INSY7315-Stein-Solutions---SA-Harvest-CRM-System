// @vitest-environment jsdom
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, cleanup, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { DocumentUploadStep } from './DocumentUploadStep';

const mutate = vi.fn();
let mockUploadState: {
  isPending: boolean;
  isError: boolean;
  error: unknown;
} = { isPending: false, isError: false, error: null };

vi.mock('../hooks/useSubmitPublicDonorDocument', () => ({
  useSubmitPublicDonorDocument: () => ({
    mutate: (
      variables: { sessionToken: string; file: File },
      opts?: { onSuccess?: () => void }
    ) => mutate(variables, opts),
    reset: vi.fn(),
    ...mockUploadState,
  }),
}));

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

function makeFile(name: string, type: string, sizeBytes = 1024) {
  const file = new File([new Uint8Array(sizeBytes)], name, { type });
  return file;
}

describe('DocumentUploadStep', () => {
  afterEach(() => {
    cleanup();
    mutate.mockReset();
    mockUploadState = { isPending: false, isError: false, error: null };
  });

  it('rejects an unsupported file type before calling the mutation', async () => {
    render(<DocumentUploadStep submissionToken="tok-1" onUploaded={vi.fn()} />, { wrapper });

    // fireEvent.change (not userEvent.upload) deliberately bypasses the
    // input's `accept` filtering — this is the drag-and-drop path, which
    // never respects `accept`, so the component's own validation is the
    // only thing standing between a wrong-type file and the API.
    const input = screen.getByLabelText(/bbbee certificate file input/i);
    fireEvent.change(input, { target: { files: [makeFile('cert.docx', 'application/msword')] } });

    expect(await screen.findByText(/unsupported file type/i)).toBeInTheDocument();
    expect(mutate).not.toHaveBeenCalled();
  });

  it('rejects a file over the 5MB limit before calling the mutation', async () => {
    const user = userEvent.setup();
    render(<DocumentUploadStep submissionToken="tok-1" onUploaded={vi.fn()} />, { wrapper });

    const input = screen.getByLabelText(/bbbee certificate file input/i);
    await user.upload(input, makeFile('cert.pdf', 'application/pdf', 6 * 1024 * 1024));

    expect(await screen.findByText(/exceeds the 5(\.0)? MB limit/i)).toBeInTheDocument();
    expect(mutate).not.toHaveBeenCalled();
  });

  it('uploads a valid file with the given session token', async () => {
    const user = userEvent.setup();
    render(<DocumentUploadStep submissionToken="tok-1" onUploaded={vi.fn()} />, { wrapper });

    const input = screen.getByLabelText(/bbbee certificate file input/i);
    await user.upload(input, makeFile('cert.pdf', 'application/pdf'));

    expect(mutate).toHaveBeenCalledWith(
      { sessionToken: 'tok-1', file: expect.objectContaining({ name: 'cert.pdf' }) },
      expect.objectContaining({ onSuccess: expect.any(Function) })
    );
  });

  it('shows an unrecoverable message and no retry option once the session token has expired', () => {
    mockUploadState = {
      isPending: false,
      isError: true,
      error: {
        isAxiosError: true,
        response: { data: { errors: [{ field: 'SessionToken', message: 'Session token expired or invalid.' }] } },
      },
    };
    render(<DocumentUploadStep submissionToken="tok-1" onUploaded={vi.fn()} />, { wrapper });

    expect(screen.getByText(/your session has expired/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /retry/i })).not.toBeInTheDocument();
  });
});
