import { useRef, useState, type ChangeEvent, type DragEvent, type KeyboardEvent } from 'react';
import { UploadCloud, Loader2, FileText } from 'lucide-react';
import { DOCUMENT_TYPE_META } from '@/features/donors/lib/documentTypes';
import { formatFileSize } from '@/features/donors/lib/donorFormatters';
import { getFieldError } from '@/lib/apiError';
import { useSubmitPublicDonorDocument } from '../hooks/useSubmitPublicDonorDocument';
import { Button } from '@/components/ui/button';

const META = DOCUMENT_TYPE_META.BBBEECertificate;

interface DocumentUploadStepProps {
  submissionToken: string;
  onUploaded: () => void;
}

/**
 * The second network call of the submission flow — runs immediately after
 * POST /public/donors/submit succeeds, using the session token it returned.
 * A donor shouldn't need to understand this is technically a separate
 * request, so it's presented as the natural next step of one continuous
 * submission rather than its own page.
 */
export function DocumentUploadStep({ submissionToken, onUploaded }: DocumentUploadStepProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [fileName, setFileName] = useState<string | null>(null);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const upload = useSubmitPublicDonorDocument();

  // A wrong documentType/MIME type/oversized file is rejected before the
  // handler ever touches the token (FluentValidation runs first), so those
  // failures are safe to retry with the same token. Only the token itself
  // being invalid/expired/already-used is unrecoverable — see
  // SubmitPublicDonorDocumentCommandHandler's token-burn semantics.
  const sessionExpired = upload.isError && getFieldError(upload.error, 'SessionToken') !== undefined;

  function validate(file: File): string | null {
    if (!META.acceptedMimeTypes.includes(file.type)) {
      return `Unsupported file type. Accepted: ${META.helperText}`;
    }
    if (file.size > META.maxSizeBytes) {
      return `File exceeds the ${formatFileSize(META.maxSizeBytes)} limit.`;
    }
    return null;
  }

  function handleFile(file: File) {
    setValidationError(null);
    upload.reset();

    const error = validate(file);
    if (error) {
      setValidationError(error);
      setFileName(null);
      setPendingFile(null);
      return;
    }

    setFileName(file.name);
    setPendingFile(file);
    upload.mutate(
      { sessionToken: submissionToken, file },
      { onSuccess: onUploaded }
    );
  }

  function retry() {
    if (pendingFile) handleFile(pendingFile);
  }

  function openPicker() {
    inputRef.current?.click();
  }

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openPicker();
    }
  }

  function onDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setIsDragging(false);
    const file = event.dataTransfer.files?.[0];
    if (file) handleFile(file);
  }

  function onFileInputChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = ''; // allow re-selecting the same file after an error
    if (file) handleFile(file);
  }

  if (sessionExpired) {
    return (
      <div className="flex flex-col items-center gap-2 rounded-[14px] border-2 border-dashed border-[#D4373A] bg-[#FDF6F6] p-7.5 text-center">
        <p className="m-0 text-[13.5px] font-bold text-[#16160F]">Your session has expired</p>
        <p className="m-0 text-[12.5px] font-medium text-[#82827A]">
          It's been too long since you submitted the form. Please start your submission again from the beginning.
        </p>
      </div>
    );
  }

  const errorMessage =
    validationError ?? (upload.isError ? (upload.error.response?.data?.message ?? 'Upload failed. Please try again.') : null);

  return (
    <div className="flex flex-col gap-3">
      <p className="-mt-1 text-[13px] font-medium text-[#82827A]">
        Almost done — upload your BBBEE certificate to finish your submission.
      </p>

      <div
        role="button"
        tabIndex={0}
        aria-label="Upload BBBEE certificate"
        onClick={openPicker}
        onKeyDown={onKeyDown}
        onDragOver={(event) => {
          event.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={onDrop}
        className={`flex cursor-pointer flex-col items-center gap-2.5 rounded-[14px] border-2 border-dashed p-7.5 text-center transition-colors ${
          isDragging ? 'border-[#16160F] bg-[#FFFDF0]' : errorMessage ? 'border-[#D4373A] bg-[#FDF6F6]' : 'border-[#E4E4DE] bg-white'
        }`}
      >
        {upload.isPending ? (
          <Loader2 className="h-4.5 w-4.5 animate-spin text-[#8A7A00]" />
        ) : fileName ? (
          <FileText className="h-4.5 w-4.5 text-[#8A7A00]" />
        ) : (
          <div className="flex h-11 w-11 items-center justify-center rounded-full bg-[#FBF6D4]">
            <UploadCloud className="h-4.5 w-4.5 text-[#8A7A00]" strokeWidth={1.8} />
          </div>
        )}
        <div className="text-[13.5px] font-bold text-[#16160F]">
          {upload.isPending ? 'Uploading…' : fileName ? fileName : 'Drag & drop your certificate, or browse'}
        </div>
        <p className="m-0 text-[11.5px] font-medium text-[#82827A]">{META.helperText}</p>
        <input
          ref={inputRef}
          type="file"
          accept={META.acceptedExtensions}
          className="sr-only"
          aria-label="BBBEE certificate file input"
          onChange={onFileInputChange}
        />
      </div>

      {errorMessage && (
        <div className="flex items-center justify-between gap-3">
          <span className="text-[11.5px] font-semibold text-[#D4373A]">{errorMessage}</span>
          {upload.isError && pendingFile && (
            <Button type="button" size="sm" variant="secondary" onClick={retry}>
              Retry
            </Button>
          )}
        </div>
      )}
    </div>
  );
}
