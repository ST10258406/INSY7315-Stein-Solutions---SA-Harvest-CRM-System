import { useState } from 'react';
import { Paperclip } from 'lucide-react';
import { getInitials, getAvatarColor } from '@/features/donors/lib/avatar';
import { toSafeHttpUrl } from '@/lib/safeUrl';
import { formatRelativeTime, interactionTypeLabel } from '../lib/interactionMeta';
import { InteractionTypeIcon } from './InteractionTypeIcon';
import type { InteractionLogDto } from '../types';

interface InteractionCardProps {
  interaction: InteractionLogDto;
  /** Hides the connector line under the timeline dot for the final row. */
  isLast: boolean;
}

export function InteractionCard({ interaction, isLast }: InteractionCardProps) {
  const [expanded, setExpanded] = useState(false);
  const authorName = interaction.createdBy?.fullName ?? 'System';
  const subject = interaction.subject?.trim() || interactionTypeLabel(interaction.interactionType);
  // Only http(s) — never render a javascript:/data: value from the API as a link.
  const safeAttachmentUrl = toSafeHttpUrl(interaction.emailAttachmentUrl);

  return (
    <div className="flex gap-4 border-t border-[var(--hair)] py-5 first:border-t-0">
      <div className="flex flex-shrink-0 flex-col items-center">
        <span className="flex h-8.5 w-8.5 items-center justify-center rounded-[11px] border border-[var(--border)] bg-[var(--icon-bg)]">
          <InteractionTypeIcon type={interaction.interactionType} className="h-4 w-4 text-[var(--icon)]" />
        </span>
        {!isLast && <span className="mt-2 min-h-1.5 w-px flex-1 bg-[var(--divider)]" aria-hidden />}
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-2.5">
          <span className="text-sm font-extrabold tracking-tight text-[var(--ink)]">{subject}</span>
          <span className="rounded-lg bg-[var(--chip)] px-2.25 py-0.75 text-[10.5px] font-bold tracking-wide text-[var(--muted-c)]">
            {interactionTypeLabel(interaction.interactionType)}
          </span>
          <span className="ml-auto whitespace-nowrap text-[11.5px] font-semibold text-[var(--muted2)]">
            {formatRelativeTime(interaction.createdAt)}
          </span>
        </div>

        <p
          className={`m-0 mt-1.75 max-w-[640px] text-13 font-medium leading-relaxed text-[var(--muted-c)] [text-wrap:pretty] ${
            expanded ? '' : 'line-clamp-2'
          }`}
        >
          {interaction.body}
        </p>

        <div className="mt-2.5 flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <span
              className="flex h-5.5 w-5.5 items-center justify-center rounded-full text-[9px] font-bold text-white"
              style={{ backgroundColor: getAvatarColor(authorName) }}
            >
              {getInitials(authorName)}
            </span>
            <span className="text-xs font-semibold text-[var(--muted-c)]">{authorName}</span>
          </div>

          {interaction.body.length > 140 && (
            <button
              type="button"
              onClick={() => setExpanded((value) => !value)}
              className="border-b-[1.5px] border-[var(--brand-yellow)] p-0 text-xs font-bold text-[var(--ink)]"
            >
              {expanded ? 'Show less' : 'Read more'}
            </button>
          )}

          {safeAttachmentUrl && (
            <a
              href={safeAttachmentUrl}
              target="_blank"
              rel="noreferrer"
              className="flex items-center gap-1.5 text-xs font-semibold text-[var(--muted-c)] hover:text-[var(--ink)]"
            >
              <Paperclip className="h-3.25 w-3.25" />
              <span>Attachment</span>
            </a>
          )}
        </div>
      </div>
    </div>
  );
}
