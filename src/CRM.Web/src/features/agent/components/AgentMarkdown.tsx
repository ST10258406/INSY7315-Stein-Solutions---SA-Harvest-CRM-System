import type { ReactNode } from 'react';

/**
 * Renders the small Markdown subset the agent is told to use — paragraphs, headings,
 * bullet/numbered lists, tables, **bold**, *italic* and `code` — straight to React
 * elements. Every piece of model text ends up as a React text node, so it is escaped:
 * no dangerouslySetInnerHTML, no HTML pass-through, and links render as their text only
 * (the agent must never send users to a URL). Anything outside the subset shows as-is.
 */
export function AgentMarkdown({ content }: { content: string }) {
  return <div className="flex flex-col gap-2">{parseBlocks(content.replace(/\r\n/g, '\n').split('\n'))}</div>;
}

const BULLET = /^\s*[-*•]\s+(.*)$/;
const NUMBERED = /^\s*\d+[.)]\s+(.*)$/;
const HEADING = /^\s*#{1,6}\s+(.*)$/;
const TABLE_ROW = /^\s*\|.*\|\s*$/;
const TABLE_DIVIDER = /^\s*\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)*\|?\s*$/;

function parseBlocks(lines: string[]): ReactNode[] {
  const blocks: ReactNode[] = [];
  let i = 0;

  const takeWhile = (test: (line: string) => boolean) => {
    const taken: string[] = [];
    while (i < lines.length && test(lines[i])) taken.push(lines[i++]);
    return taken;
  };

  while (i < lines.length) {
    const line = lines[i];
    const key = blocks.length;

    if (!line.trim()) {
      i++;
    } else if (HEADING.test(line)) {
      blocks.push(
        <p key={key} className="m-0 font-extrabold">
          {inline(line.replace(HEADING, '$1'))}
        </p>,
      );
      i++;
    } else if (BULLET.test(line)) {
      blocks.push(
        <ul key={key} className="m-0 list-disc space-y-1 pl-5">
          {takeWhile((l) => BULLET.test(l)).map((l, n) => (
            <li key={n}>{inline(l.replace(BULLET, '$1'))}</li>
          ))}
        </ul>,
      );
    } else if (NUMBERED.test(line)) {
      blocks.push(
        <ol key={key} className="m-0 list-decimal space-y-1 pl-5">
          {takeWhile((l) => NUMBERED.test(l)).map((l, n) => (
            <li key={n}>{inline(l.replace(NUMBERED, '$1'))}</li>
          ))}
        </ol>,
      );
    } else if (TABLE_ROW.test(line) && TABLE_DIVIDER.test(lines[i + 1] ?? '')) {
      const [header, , ...rows] = takeWhile((l) => TABLE_ROW.test(l) || TABLE_DIVIDER.test(l)).map(cells);
      blocks.push(
        <div key={key} className="overflow-x-auto">
          <table className="w-full border-collapse text-left text-[12.5px]">
            <thead>
              <tr>
                {header.map((cell, n) => (
                  <th key={n} className="border-b border-border px-2 py-1 font-bold">
                    {inline(cell)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, r) => (
                <tr key={r}>
                  {row.map((cell, n) => (
                    <td key={n} className="border-b border-border/60 px-2 py-1">
                      {inline(cell)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>,
      );
    } else {
      // Always consumes the current line, so e.g. a lone "| a |" row can't stall the loop.
      const paragraph = [
        lines[i++],
        ...takeWhile((l) => !!l.trim() && !HEADING.test(l) && !BULLET.test(l) && !NUMBERED.test(l) && !TABLE_ROW.test(l)),
      ];
      blocks.push(
        <p key={key} className="m-0">
          {paragraph.map((l, n) => (
            <span key={n}>
              {n > 0 && <br />}
              {inline(l.trim())}
            </span>
          ))}
        </p>,
      );
    }
  }

  return blocks;
}

function cells(row: string): string[] {
  return row.trim().replace(/^\|/, '').replace(/\|$/, '').split('|').map((c) => c.trim());
}

// **bold**, `code`, *italic* / _italic_, and [text](url) → text only.
const INLINE = /(\*\*[^*]+\*\*|`[^`]+`|\*[^*\s][^*]*\*|_[^_\s][^_]*_|\[[^\]]+\]\([^)]*\))/g;

function inline(text: string): ReactNode[] {
  return text.split(INLINE).map((part, n) => {
    if (part.startsWith('**') && part.endsWith('**') && part.length > 4) return <strong key={n}>{part.slice(2, -2)}</strong>;
    if (part.startsWith('`') && part.endsWith('`') && part.length > 2)
      return (
        <code key={n} className="rounded bg-hover px-1 text-[12.5px]">
          {part.slice(1, -1)}
        </code>
      );
    if (/^\[[^\]]+\]\([^)]*\)$/.test(part)) return part.slice(1, part.indexOf(']'));
    if (/^([*_]).+\1$/.test(part) && part.length > 2) return <em key={n}>{part.slice(1, -1)}</em>;
    return part;
  });
}
