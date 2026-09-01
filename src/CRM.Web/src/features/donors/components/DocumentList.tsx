import { useState } from 'react';
import { FileText, Download, Trash2, Loader2 } from 'lucide-react';
import { RoleGuard } from '@/features/auth/components/RoleGuard';
import { useDownloadDonorDocument } from '../hooks/useDownloadDonorDocument';
import { useDeleteDonorDocument } from '../hooks/useDeleteDonorDocument';
import { ADMIN_ROLES, DOCUMENT_TYPE_META, isRestrictedDocumentType } from '../lib/documentTypes';
import { formatDate, formatFileSize } from '../lib/donorFormatters';
import type { DonorDocumentDto } from '../types';
import { Button } from '@/components/ui/button';

interface DocumentListProps {
  donorId: string;
  documents: DonorDocumentDto[];
}

export function DocumentList({ donorId, documents }: DocumentListProps) {
  const download = useDownloadDonorDocument(donorId);
  const remove = useDeleteDonorDocument(donorId);
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);

  if (documents.length === 0) {
    return <p className="mt-4 text-sm text-muted-foreground">No documents uploaded for this donor.</p>;
  }

  return (
    <ul className="mt-4 flex flex-col gap-2">
      {documents.map((doc) => {
        const meta = DOCUMENT_TYPE_META[doc.documentType];
        const isDownloadingThis = download.isPending && download.variables?.documentId === doc.id;
        const isDeletingThis = remove.isPending && remove.variables?.documentId === doc.id;
        const downloadFailed = download.isError && download.variables?.documentId === doc.id;
        const deleteFailed = remove.isError && remove.variables?.documentId === doc.id;

        const downloadButton = (
          <Button
            variant="secondary"
            size="icon"
            onClick={() => download.mutate({ documentId: doc.id })}
            disabled={isDownloadingThis}
            aria-label={`Download ${doc.originalFileName}`}
            title="Download"
            className="h-8 w-8 shrink-0 border-border text-muted-foreground hover:border-foreground/60 hover:text-foreground disabled:opacity-50"
          >
            {isDownloadingThis ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Download className="h-3.5 w-3.5" />}
          </Button>
        );

        return (
          <li key={doc.id} className="flex flex-col gap-2 rounded-lg border border-border bg-secondary px-3 py-2.5">
            <div className="flex items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-2.5">
                <FileText className="h-4 w-4 shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-foreground">{doc.originalFileName}</p>
                  <p className="text-xs text-muted-foreground">
                    {meta?.label ?? doc.documentType} · {formatFileSize(doc.fileSizeBytes)} · Uploaded{' '}
                    {formatDate(doc.uploadedAt)}
                  </p>
                </div>
              </div>

              <div className="flex shrink-0 items-center gap-1.5">
                {!doc.isActive && (
                  <span className="rounded-full border border-border px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                    Archived
                  </span>
                )}
                {isRestrictedDocumentType(doc.documentType) ? (
                  <RoleGuard allowedRoles={ADMIN_ROLES}>{downloadButton}</RoleGuard>
                ) : (
                  downloadButton
                )}
                <RoleGuard allowedRoles={ADMIN_ROLES}>
                  <Button
                    variant="secondary"
                    size="icon"
                    onClick={() => setPendingDeleteId(doc.id)}
                    disabled={isDeletingThis}
                    aria-label={`Delete ${doc.originalFileName}`}
                    title="Delete"
                    className="h-8 w-8 shrink-0 border-border text-muted-foreground hover:border-destructive/60 hover:text-destructive disabled:opacity-50"
                  >
                    {isDeletingThis ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Trash2 className="h-3.5 w-3.5" />}
                  </Button>
                </RoleGuard>
              </div>
            </div>

            {pendingDeleteId === doc.id && (
              <div className="flex items-center justify-between gap-3 rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2">
                <p className="m-0 text-xs font-medium text-destructive">Delete this document? This can't be undone.</p>
                <div className="flex shrink-0 items-center gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setPendingDeleteId(null)}
                    className="h-8 text-xs font-medium text-muted-foreground hover:text-foreground"
                  >
                    Cancel
                  </Button>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => {
                      remove.mutate({ documentId: doc.id });
                      setPendingDeleteId(null);
                    }}
                    className="h-8 px-3 py-1 text-xs font-bold rounded-full"
                  >
                    Delete
                  </Button>
                </div>
              </div>
            )}

            {downloadFailed && (
              <p className="m-0 text-xs text-destructive">
                {download.error?.response?.data?.message ?? 'Could not get a download link. Please try again.'}
              </p>
            )}
            {deleteFailed && (
              <p className="m-0 text-xs text-destructive">
                {remove.error?.response?.data?.message ?? 'Could not delete this document. Please try again.'}
              </p>
            )}
          </li>
        );
      })}
    </ul>
  );
}
