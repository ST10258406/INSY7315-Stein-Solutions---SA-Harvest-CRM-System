import { useRef, useState, type ChangeEvent, type DragEvent, type KeyboardEvent } from 'react';
import { UploadCloud, Loader2 } from 'lucide-react';
import { useUploadDonorDocument } from '../hooks/useUploadDonorDocument';
import { DOCUMENT_TYPE_META } from '../lib/documentTypes';
import { formatFileSize } from '../lib/donorFormatters';
import type { DocumentTypeCode } from '../types';

interface DocumentUploadProps {
  donorId: string;
  documentType: DocumentTypeCode;
}

/**
 * Drag-and-drop / file-picker upload for a single document type. Validates
 * type/size client-side before ever hitting the API — a UX nicety mirroring
 * UploadDonorDocumentCommandValidator; the backend still re-checks and is
 * the real source of truth (see documentTypes.ts).
 */
export function DocumentUpload({ donorId, documentType }: DocumentUploadProps) {
  const meta = DOCUMENT_TYPE_META[documentType];
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const upload = useUploadDonorDocument(donorId);

  function validate(file: File): string | null {
    if (!meta.acceptedMimeTypes.includes(file.type)) {
      return `Unsupported file type for ${meta.label}. Accepted: ${meta.helperText}`;
    }
    if (file.size > meta.maxSizeBytes) {
      return `File exceeds the ${formatFileSize(meta.maxSizeBytes)} limit.`;
    }
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
    upload.mutate({ documentType, file });
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

  const errorMessage =
    validationError ?? (upload.isError ? (upload.error.response?.data?.message ?? 'Upload failed. Please try again.') : null);

  return (
    <div className="flex flex-col gap-2">
      <span className="text-xs font-bold text-foreground">{meta.label}</span>
      <div
        role="button"
        tabIndex={0}
        aria-label={`Upload ${meta.label}`}
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
        <p className="m-0 text-[13px] font-bold text-foreground">
          {upload.isPending ? 'Uploading…' : 'Drop file here or browse'}
        </p>
        <p className="m-0 text-[11.5px] font-medium text-muted-foreground">{meta.helperText}</p>
        <input
          ref={inputRef}
          type="file"
          accept={meta.acceptedExtensions}
          className="sr-only"
          aria-label={`${meta.label} file input`}
          onChange={onFileInputChange}
        />
      </div>
      {errorMessage && <p className="m-0 text-xs text-destructive">{errorMessage}</p>}
      {upload.isSuccess && !errorMessage && <p className="m-0 text-xs text-muted-foreground">Uploaded.</p>}
    </div>
  );
}
