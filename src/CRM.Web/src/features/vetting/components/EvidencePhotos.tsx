import { Camera, Eye } from 'lucide-react';
import { FileUpload } from '@/components/common/FileUpload';
import { Button } from '@/components/ui/button';
import { useUploadVettingFile, useVettingFileDownload, useVettingPhotoUrl } from '../hooks';
import { PHOTO_CATEGORY_LABELS } from '../lib/vettingLabels';
import { MAX_UPLOAD_BYTES, PHOTO_MIME_TYPES } from '../lib/vettingUploads';
import { useSectionOpenedOnce } from './form/accordionOpenContext';
import { SaveFirstPlaceholder } from './AssessmentFileSlot';
import type { VettingPhoto } from '../types';

const TIME = new Intl.DateTimeFormat('en-ZA', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });

const capturedCount = (photos: VettingPhoto[]) => photos.filter((p) => p.isCaptured).length;

/** Header badge for section 12: "3 / 4 captured", red while any slot is missing. */
export function PhotosCapturedBadge({ photos }: { photos: VettingPhoto[] }) {
  const captured = capturedCount(photos);
  const complete = captured === 4;
  return (
    <span
      className={`shrink-0 rounded-full px-2.5 py-0.5 text-xs font-bold ${
        complete ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-400' : 'bg-destructive/10 text-destructive'
      }`}
    >
      {captured} / 4 captured
    </span>
  );
}

interface EvidencePhotosProps {
  /** Undefined until the form has been saved once. */
  submissionId?: string;
  photos: VettingPhoto[];
  editable: boolean;
  /** The accordion section id; thumbnails load only once it has been opened. */
  sectionId?: string;
}

/** The 4 mandatory evidence photo tiles. The camera opens directly on phones and tablets. */
export function EvidencePhotos({ submissionId, photos, editable, sectionId = 'photos' }: EvidencePhotosProps) {
  const opened = useSectionOpenedOnce(sectionId);
  if (!submissionId) return <div className="sm:col-span-2"><SaveFirstPlaceholder /></div>;

  return (
    <div className="grid grid-cols-1 gap-3.5 sm:col-span-2 sm:grid-cols-2">
      {[...photos].sort((a, b) => a.shotIndex - b.shotIndex).map((photo) => (
        <PhotoTile key={photo.shotIndex} submissionId={submissionId} photo={photo} editable={editable} loadThumbnail={opened} />
      ))}
    </div>
  );
}

function PhotoTile({ submissionId, photo, editable, loadThumbnail }: {
  submissionId: string; photo: VettingPhoto; editable: boolean; loadThumbnail: boolean;
}) {
  const label = PHOTO_CATEGORY_LABELS[photo.category];
  const upload = useUploadVettingFile(submissionId, { shotIndex: photo.shotIndex });
  const download = useVettingFileDownload(submissionId);
  const thumbnail = useVettingPhotoUrl(submissionId, photo, loadThumbnail);

  return (
    <div data-testid={`photo-${photo.category}`} className="flex flex-col gap-2.5 rounded-2xl border border-border bg-background p-3.5">
      <div className="flex items-center gap-2">
        <Camera className="h-4 w-4 text-muted-foreground" />
        <span className="text-[13px] font-bold text-foreground">{label}</span>
        <span
          className={`ml-auto rounded-full px-2 py-0.5 text-[11px] font-bold ${
            photo.isCaptured ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-400' : 'bg-destructive/10 text-destructive'
          }`}
        >
          {photo.isCaptured ? 'Captured' : 'Missing'}
        </span>
      </div>

      {photo.isCaptured && (
        <>
          <div className="flex aspect-[4/3] items-center justify-center overflow-hidden rounded-xl bg-muted">
            {thumbnail.data ? (
              <img src={thumbnail.data} alt={`${label} evidence photo`} className="h-full w-full object-cover" />
            ) : (
              <span className="text-xs font-medium text-muted-foreground">{thumbnail.isError ? 'Preview unavailable' : 'Loading preview…'}</span>
            )}
          </div>
          <div className="flex items-center gap-2">
            <span className="text-[11.5px] font-medium text-muted-foreground">
              {photo.capturedAt ? `Captured ${TIME.format(new Date(photo.capturedAt))}` : 'Captured'}
            </span>
            <Button type="button" variant="outline" size="sm" className="ml-auto" onClick={() => download.mutate({ shotIndex: photo.shotIndex })} aria-label={`View ${label} photo`}>
              <Eye className="h-3.5 w-3.5" /> View
            </Button>
          </div>
        </>
      )}

      {editable && (
        <FileUpload
          label={photo.isCaptured ? `Replace ${label.toLowerCase()} photo` : `${label} photo`}
          acceptedMimeTypes={PHOTO_MIME_TYPES}
          maxSizeBytes={MAX_UPLOAD_BYTES}
          helperText="JPG or PNG, up to 5MB."
          capture="environment"
          upload={upload}
        />
      )}
    </div>
  );
}
