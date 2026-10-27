import { render, screen, cleanup, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { FileUpload, type FileUploadMutation } from './FileUpload';

function renderUpload(overrides: Partial<FileUploadMutation> = {}) {
  const upload: FileUploadMutation = { mutate: vi.fn(), isPending: false, isSuccess: false, error: null, reset: vi.fn(), ...overrides };
  render(
    <FileUpload
      label="Kitchen photo"
      acceptedMimeTypes={['image/jpeg', 'image/png']}
      maxSizeBytes={5 * 1024 * 1024}
      helperText="JPG or PNG, up to 5MB."
      capture="environment"
      upload={upload}
    />,
  );
  return upload;
}

describe('FileUpload', () => {
  afterEach(cleanup);

  it('rejects a wrong type client-side without calling upload', () => {
    const upload = renderUpload();
    // fireEvent bypasses the input's accept filter, like a drag-and-drop would.
    fireEvent.change(screen.getByLabelText('Kitchen photo file input'), {
      target: { files: [new File(['%PDF'], 'kitchen.pdf', { type: 'application/pdf' })] },
    });
    expect(screen.getByText(/Unsupported file type for Kitchen photo/)).toBeInTheDocument();
    expect(upload.mutate).not.toHaveBeenCalled();
  });

  it('rejects an oversize file client-side without calling upload', async () => {
    const upload = renderUpload();
    await userEvent.setup().upload(
      screen.getByLabelText('Kitchen photo file input'),
      new File([new Uint8Array(6 * 1024 * 1024)], 'big.jpg', { type: 'image/jpeg' }),
    );
    expect(screen.getByText(/exceeds the 5.0 MB limit/i)).toBeInTheDocument();
    expect(upload.mutate).not.toHaveBeenCalled();
  });

  it('calls upload.mutate with a valid file, and asks the phone for the rear camera', async () => {
    const upload = renderUpload();
    const file = new File(['jpeg'], 'kitchen.jpg', { type: 'image/jpeg' });
    const input = screen.getByLabelText('Kitchen photo file input');
    expect(input).toHaveAttribute('capture', 'environment');
    expect(input).toHaveAttribute('accept', 'image/jpeg,image/png');

    await userEvent.setup().upload(input, file);

    expect(upload.reset).toHaveBeenCalled();
    expect(upload.mutate).toHaveBeenCalledWith(file);
  });

  it('shows the server message when the upload failed', () => {
    renderUpload({ error: { response: { data: { message: 'Uploads are only allowed while the submission is New or Returned.' } } } as never });
    expect(screen.getByText('Uploads are only allowed while the submission is New or Returned.')).toBeInTheDocument();
  });
});
