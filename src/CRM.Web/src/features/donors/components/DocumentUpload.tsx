import { FileUpload } from '@/components/common/FileUpload';
import { useUploadDonorDocument } from '../hooks/useUploadDonorDocument';
import { DOCUMENT_TYPE_META } from '../lib/documentTypes';
import type { DocumentTypeCode } from '../types';

interface DocumentUploadProps {
  donorId: string;
  documentType: DocumentTypeCode;
}

/** A donor document slot: the shared FileUpload driven by the donor upload hook and its type's limits. */
export function DocumentUpload({ donorId, documentType }: DocumentUploadProps) {
  const meta = DOCUMENT_TYPE_META[documentType];
  const upload = useUploadDonorDocument(donorId);

  return (
    <FileUpload
      label={meta.label}
      acceptedMimeTypes={meta.acceptedMimeTypes}
      maxSizeBytes={meta.maxSizeBytes}
      helperText={meta.helperText}
      upload={{ ...upload, mutate: (file) => upload.mutate({ documentType, file }) }}
    />
  );
}
