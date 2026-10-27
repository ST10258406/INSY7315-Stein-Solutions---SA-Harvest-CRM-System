import { Fragment, type ReactNode } from 'react';

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
// Divider cells: dashes with optional alignment colons. gpt-oss often writes Unicode dashes
// (‐ ‑ ‒ – — ― −) instead of '-', so accept those too.
const DIVIDER_CELL = /^:?[-\u2010-\u2015\u2212]+:?$/;
const PIPE = /(?<!\\)\|/;
const BR = /<br\s*\/?>/i;
const NUMERIC = /^[-+]?(R\s?)?[\d\s.,]+%?$/;

type Align = 'left' | 'center' | 'right';

function parseBlocks(lines: string[]): ReactNode[] {
  const blocks: ReactNode[] = [];
  let i = 0;

  const takeWhile = (test: (line: string) => boolean) => {
    const taken: string[] = [];
    while (i < lines.length && test(lines[i])) taken.push(lines[i++]);
    return taken;
  };
  const tableStartsAt = (n: number) => PIPE.test(lines[n] ?? '') && isDivider(lines[n + 1] ?? '');

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
    } else if (tableStartsAt(i)) {
      const header = cells(lines[i]);
      const aligns = alignments(lines[i + 1], header.length);
      i += 2;
      const rows = takeWhile((l) => PIPE.test(l) && !!l.trim())
        .filter((l) => !isDivider(l))
        .map((l) => fit(cells(l), header.length));
      header.forEach((_, c) => {
        const column = rows.map((row) => row[c]).filter(Boolean);
        if (!aligns[c] && column.length > 0 && column.every((cell) => NUMERIC.test(cell))) aligns[c] = 'right';
      });
      blocks.push(
        <div key={key} className="overflow-x-auto rounded-lg border border-border">
          <table className="w-full border-collapse text-left text-[12.5px]">
            <thead className="bg-hover">
              <tr>
                {header.map((cell, n) => (
                  <th key={n} style={{ textAlign: aligns[n] }} className="whitespace-nowrap px-2.5 py-1.5 font-bold">
                    {cellContent(cell)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, r) => (
                <tr key={r} className="border-t border-border/70 even:bg-hover/40">
                  {row.map((cell, n) => (
                    <td key={n} style={{ textAlign: aligns[n] }} className="px-2.5 py-1.5 align-top">
                      {cellContent(cell)}
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
        ...takeWhile((l) => !!l.trim() && !HEADING.test(l) && !BULLET.test(l) && !NUMBERED.test(l) && !tableStartsAt(i)),
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

// Outer pipes are optional, as in GFM; `\|` is a literal pipe inside a cell.
function cells(row: string): string[] {
  return row
    .trim()
    .replace(/^\|/, '')
    .replace(/(?<!\\)\|$/, '')
    .split(PIPE)
    .map((c) => c.replace(/\\\|/g, '|').trim());
}

function isDivider(line: string): boolean {
  if (!/[-\u2010-\u2015\u2212]/.test(line)) return false;
  const parts = cells(line);
  return (PIPE.test(line) || parts.length > 1) && parts.every((c) => DIVIDER_CELL.test(c.replace(/\s+/g, '')));
}

function alignments(divider: string, width: number): (Align | undefined)[] {
  return fit(cells(divider), width).map((c) => {
    const left = c.startsWith(':');
    const right = c.endsWith(':') && c.length > 1;
    return left && right ? 'center' : right ? 'right' : left ? 'left' : undefined;
  });
}

// Ragged rows are padded or trimmed to the header width.
function fit(row: string[], width: number): string[] {
  return Array.from({ length: width }, (_, n) => row[n] ?? '');
}

// `<br>` in a cell becomes a React line break; the rest stays escaped text.
function cellContent(cell: string): ReactNode[] {
  return cell.split(BR).map((part, n) => (
    <Fragment key={n}>
      {n > 0 && <br />}
      {inline(part.trim())}
    </Fragment>
  ));
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
