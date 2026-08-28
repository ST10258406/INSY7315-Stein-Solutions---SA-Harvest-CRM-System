import type { DocumentTypeCode } from '../types';

/** Roles that clear the backend's "AdminOrAbove" policy. */
export const ADMIN_ROLES = ['Admin', 'SuperAdmin'];

export interface DocumentTypeMeta {
  label: string;
  acceptedMimeTypes: string[];
  /** Value for an <input type="file" accept="..."> covering the same set. */
  acceptedExtensions: string;
  maxSizeBytes: number;
  helperText: string;
}

// Mirrors UploadDonorDocumentCommandValidator's AllowedMimeTypes/MaxFileSizeBytes
// (CRM.Application.Modules.Donors.Commands.UploadDonorDocument) — this is a
// client-side UX nicety only, the backend rule is the real source of truth
// and re-validates regardless. Keep the two in sync.
export const DOCUMENT_TYPE_META: Record<DocumentTypeCode, DocumentTypeMeta> = {
  BBBEECertificate: {
    label: 'BBBEE Certificate',
    acceptedMimeTypes: ['application/pdf', 'image/jpeg', 'image/png'],
    acceptedExtensions: '.pdf,.jpg,.jpeg,.png',
    maxSizeBytes: 5 * 1024 * 1024,
    helperText: 'PDF, JPG or PNG, up to 5MB.',
  },
  Signature: {
    label: 'Signature',
    acceptedMimeTypes: ['image/png'],
    acceptedExtensions: '.png',
    maxSizeBytes: 5 * 1024 * 1024,
    helperText: 'PNG only, up to 5MB.',
  },
};

/**
 * Mirrors DocumentTypeAuthorizationHandler (CRM.API.Authorization) — only
 * BBBEE certificates need the elevated Admin/SuperAdmin role to download;
 * everything else only needs the endpoint's base ProcurementOrAbove gate,
 * which viewing this page already implies.
 */
export function isRestrictedDocumentType(documentType: DocumentTypeCode): boolean {
  return documentType === 'BBBEECertificate';
}
