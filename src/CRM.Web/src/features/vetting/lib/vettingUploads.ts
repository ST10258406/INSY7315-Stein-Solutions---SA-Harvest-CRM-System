import type { VettingFiles, VettingFileSlot } from '../types';

// Mirrors VettingUploadSlots (CRM.Application, #158). The server re-checks the real bytes.
const PDF_OR_IMAGE = ['application/pdf', 'image/jpeg', 'image/png'];
const IMAGE_ONLY = ['image/jpeg', 'image/png'];
export const MAX_UPLOAD_BYTES = 5 * 1024 * 1024;

export interface FileSlotMeta {
  label: string;
  acceptedMimeTypes: string[];
  helperText: string;
  /** The VettingFiles flag that says this slot is filled. */
  filledKey: keyof VettingFiles;
}

export const FILE_SLOT_META: Record<VettingFileSlot, FileSlotMeta> = {
  'npo-certificate': { label: 'NPO certificate', acceptedMimeTypes: PDF_OR_IMAGE, helperText: 'PDF, JPG or PNG, up to 5MB.', filledKey: 'npoCertificate' },
  'pbo-certificate': { label: 'PBO certificate', acceptedMimeTypes: PDF_OR_IMAGE, helperText: 'PDF, JPG or PNG, up to 5MB.', filledKey: 'pboCertificate' },
  'kitchen-storage-photos': { label: 'Kitchen & storage photos', acceptedMimeTypes: IMAGE_ONLY, helperText: 'JPG or PNG, up to 5MB.', filledKey: 'kitchenStoragePhotos' },
  'facility-photos': { label: 'Facility photos', acceptedMimeTypes: IMAGE_ONLY, helperText: 'JPG or PNG, up to 5MB.', filledKey: 'facilityPhotos' },
  certificates: { label: 'Combined certificates (NPO/PBO)', acceptedMimeTypes: PDF_OR_IMAGE, helperText: 'PDF, JPG or PNG, up to 5MB.', filledKey: 'certificates' },
};

export const PHOTO_MIME_TYPES = IMAGE_ONLY;
