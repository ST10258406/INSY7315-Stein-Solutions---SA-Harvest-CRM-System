import { FileText } from 'lucide-react';
import type { DonorDocumentDto } from '../types';
import { formatDate, formatFileSize } from '../lib/donorFormatters';

interface DonorDocumentsSectionProps {
  documents: DonorDocumentDto[];
}

/**
 * Read-only list of documents already on the donor record — the data is part
 * of DonorDetailDto so it's real, not a placeholder. Upload/download actions
 * are deliberately not wired here: those are dedicated reusable components
 * from Issue 44, which don't exist yet. This section is the integration
 * point they'll slot into.
 */
export function DonorDocumentsSection({ documents }: DonorDocumentsSectionProps) {
  return (
    <div className="rounded-2xl border border-[#2B2B23] bg-[#141410] p-5">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold tracking-wide text-[#B9B9AE] uppercase">Documents</h3>
        <span
          title="Upload/download tools ship with Issue 44"
          className="rounded-full border border-[#2B2B23] px-2.5 py-0.5 text-[11px] font-medium text-[#6B6B60]"
        >
          Upload coming soon
        </span>
      </div>

      {documents.length === 0 ? (
        <p className="mt-4 text-sm text-[#6B6B60]">No documents uploaded for this donor.</p>
      ) : (
        <ul className="mt-4 flex flex-col gap-2">
          {documents.map((doc) => (
            <li
              key={doc.id}
              className="flex items-center justify-between gap-3 rounded-lg border border-[#2B2B23] bg-[#1A1A14] px-3 py-2.5"
            >
              <div className="flex min-w-0 items-center gap-2.5">
                <FileText className="h-4 w-4 shrink-0 text-[#6B6B60]" />
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-[#F4F4EE]">{doc.originalFileName}</p>
                  <p className="text-xs text-[#6B6B60]">
                    {doc.documentType} · {formatFileSize(doc.fileSizeBytes)} · Uploaded {formatDate(doc.uploadedAt)}
                  </p>
                </div>
              </div>
              {!doc.isActive && (
                <span className="shrink-0 rounded-full border border-[#2B2B23] px-2 py-0.5 text-[11px] font-medium text-[#6B6B60]">
                  Archived
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
