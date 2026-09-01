import { RoleGuard } from '@/features/auth/components/RoleGuard';
import { DocumentUpload } from './DocumentUpload';
import { DocumentList } from './DocumentList';
import { ADMIN_ROLES } from '../lib/documentTypes';
import type { DonorDocumentDto } from '../types';

interface DonorDocumentsSectionProps {
  donorId: string;
  documents: DonorDocumentDto[];
}

export function DonorDocumentsSection({ donorId, documents }: DonorDocumentsSectionProps) {
  return (
    <div className="rounded-2xl border border-border bg-card p-5">
      <h3 className="text-xs font-semibold tracking-wide text-muted-foreground uppercase">Documents</h3>

      {/* Upload is Admin/SuperAdmin-only for both document types — mirrors the
          backend's blanket [Authorize(Policy = "AdminOrAbove")] on the whole
          POST /documents endpoint (DonorsController), not just BBBEE. */}
      <RoleGuard allowedRoles={ADMIN_ROLES}>
        <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <DocumentUpload donorId={donorId} documentType="BBBEECertificate" />
          <DocumentUpload donorId={donorId} documentType="Signature" />
        </div>
      </RoleGuard>

      <DocumentList donorId={donorId} documents={documents} />
    </div>
  );
}
