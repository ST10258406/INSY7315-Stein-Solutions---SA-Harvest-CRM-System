import { useRef, useState, type ChangeEvent, type DragEvent, type KeyboardEvent } from 'react';
import { UploadCloud, Loader2 } from 'lucide-react';
import type { AxiosError } from 'axios';
import { formatFileSize } from '@/features/donors/lib/donorFormatters';
import type { ApiErrorEnvelope } from '@/lib/apiError';

/** The slice of a TanStack mutation that FileUpload drives; callers adapt their own variables in `mutate`. */
export interface FileUploadMutation {
  mutate: (file: File) => void;
  isPending: boolean;
  isSuccess: boolean;
  error: AxiosError<ApiErrorEnvelope> | null;
  reset: () => void;
}

interface FileUploadProps {
  label: string;
  acceptedMimeTypes: string[];
  maxSizeBytes: number;
  helperText: string;
  upload: FileUploadMutation;
  /** "environment" opens the rear camera directly on phones and tablets. */
  capture?: 'environment' | 'user';
}

/**
 * Drag-and-drop / file-picker upload for one file. Validates type and size client-side before
 * calling `upload.mutate`; the backend re-checks the real bytes and is the source of truth.
 */
export function FileUpload({ label, acceptedMimeTypes, maxSizeBytes, helperText, upload, capture }: FileUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  function validate(file: File): string | null {
    if (!acceptedMimeTypes.includes(file.type)) return `Unsupported file type for ${label}. Accepted: ${helperText}`;
    if (file.size > maxSizeBytes) return `File exceeds the ${formatFileSize(maxSizeBytes)} limit.`;
    return null;
  }

  function handleFile(file: File) {
    setValidationError(null);
    upload.reset();
    const error = validate(file);
    if (error) {
      setValidationError(error);
      return;
    }
    upload.mutate(file);
  }

  const openPicker = () => inputRef.current?.click();

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

  const errorMessage =
    validationError ?? (upload.error ? (upload.error.response?.data?.message ?? 'Upload failed. Please try again.') : null);

  return (
    <div className="flex flex-col gap-2">
      <span className="text-xs font-bold text-foreground">{label}</span>
      <div
        role="button"
        tabIndex={0}
        aria-label={`Upload ${label}`}
        onClick={openPicker}
        onKeyDown={onKeyDown}
        onDragOver={(event) => {
          event.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={onDrop}
        className={`flex cursor-pointer flex-col items-center gap-1.5 rounded-2xl border-2 border-dashed p-5 text-center transition-colors ${
          isDragging ? 'border-brand bg-brand/10' : 'border-border bg-background hover:border-foreground/40'
        }`}
      >
        {upload.isPending ? (
          <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
        ) : (
          <UploadCloud className="h-5 w-5 text-muted-foreground" />
        )}
        <p className="m-0 text-[13px] font-bold text-foreground">{upload.isPending ? 'Uploading…' : 'Drop file here or browse'}</p>
        <p className="m-0 text-[11.5px] font-medium text-muted-foreground">{helperText}</p>
        <input
          ref={inputRef}
          type="file"
          accept={acceptedMimeTypes.join(',')}
          capture={capture}
          className="sr-only"
          aria-label={`${label} file input`}
          onChange={onFileInputChange}
        />
      </div>
      {errorMessage && <p className="m-0 text-xs text-destructive">{errorMessage}</p>}
      {upload.isSuccess && !errorMessage && <p className="m-0 text-xs text-muted-foreground">Uploaded.</p>}
    </div>
  );
}
