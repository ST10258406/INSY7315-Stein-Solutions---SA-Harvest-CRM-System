import { useState } from 'react';
import { Eye, FileCheck2, RefreshCw } from 'lucide-react';
import { FileUpload } from '@/components/common/FileUpload';
import { Button } from '@/components/ui/button';
import { useUploadVettingFile, useVettingFileDownload } from '../hooks';
import { FILE_SLOT_META, MAX_UPLOAD_BYTES } from '../lib/vettingUploads';
import type { VettingFiles, VettingFileSlot } from '../types';

interface AssessmentFileSlotProps {
  /** Undefined until the form has been saved once: uploads need a submission id. */
  submissionId?: string;
  slot: VettingFileSlot;
  files?: VettingFiles;
  editable: boolean;
  /** Shown while the slot is empty, e.g. "Required if registered NPO". */
  hint?: string;
}

export function SaveFirstPlaceholder() {
  return (
    <p className="m-0 rounded-2xl border border-dashed border-border bg-background p-4 text-center text-[12.5px] font-semibold text-muted-foreground">
      Save the form first to add files and photos.
    </p>
  );
}

/** One of the 5 assessment file slots: upload when empty, View + Replace when filled. */
export function AssessmentFileSlot({ submissionId, slot, files, editable, hint }: AssessmentFileSlotProps) {
  const meta = FILE_SLOT_META[slot];
  const filled = files?.[meta.filledKey] ?? false;
  const [replacing, setReplacing] = useState(false);

  return (
    <div className="flex flex-col gap-2 sm:col-span-2">
      {!submissionId ? (
        <>
          <span className="text-xs font-bold text-foreground">{meta.label}</span>
          <SaveFirstPlaceholder />
        </>
      ) : filled && !replacing ? (
        <FilledSlot submissionId={submissionId} slot={slot} label={meta.label} editable={editable} onReplace={() => setReplacing(true)} />
      ) : editable ? (
        <SlotUpload submissionId={submissionId} slot={slot} onUploaded={() => setReplacing(false)} />
      ) : (
        <>
          <span className="text-xs font-bold text-foreground">{meta.label}</span>
          <span className="text-[13px] font-medium text-muted-foreground">Not uploaded</span>
        </>
      )}
      {submissionId && !filled && hint && <span className="text-xs font-semibold text-amber-700 dark:text-amber-400">{hint}</span>}
    </div>
  );
}

function FilledSlot({ submissionId, slot, label, editable, onReplace }: {
  submissionId: string; slot: VettingFileSlot; label: string; editable: boolean; onReplace: () => void;
}) {
  const download = useVettingFileDownload(submissionId);
  return (
    <>
      <span className="text-xs font-bold text-foreground">{label}</span>
      <div className="flex flex-wrap items-center gap-2.5 rounded-2xl border border-border bg-background px-4 py-3">
        <FileCheck2 className="h-4 w-4 text-emerald-600" />
        {/* ponytail: the API keeps only the blob path for these slots, not the uploaded file name. */}
        <span className="text-[13px] font-semibold text-foreground">File uploaded</span>
        <div className="ml-auto flex gap-2">
          <Button type="button" variant="outline" size="sm" onClick={() => download.mutate({ slot })} disabled={download.isPending} aria-label={`View ${label}`}>
            <Eye className="h-3.5 w-3.5" /> View
          </Button>
          {editable && (
            <Button type="button" variant="outline" size="sm" onClick={onReplace} aria-label={`Replace ${label}`}>
              <RefreshCw className="h-3.5 w-3.5" /> Replace
            </Button>
          )}
        </div>
      </div>
      {download.error && <span className="text-xs text-destructive">{download.error.response?.data?.message ?? 'Could not open this file.'}</span>}
    </>
  );
}

function SlotUpload({ submissionId, slot, onUploaded }: { submissionId: string; slot: VettingFileSlot; onUploaded: () => void }) {
  const meta = FILE_SLOT_META[slot];
  const upload = useUploadVettingFile(submissionId, { slot });
  return (
    <FileUpload
      label={meta.label}
      acceptedMimeTypes={meta.acceptedMimeTypes}
      maxSizeBytes={MAX_UPLOAD_BYTES}
      helperText={meta.helperText}
      upload={{ ...upload, mutate: (file) => upload.mutate(file, { onSuccess: onUploaded }) }}
    />
  );
}
