/**
 * One CSV cell: always quoted, quotes doubled, and a leading = + - @ (or tab/CR)
 * neutralised with an apostrophe so a donor named e.g. "=HYPERLINK(...)" can't
 * run as a formula when the file is opened in Excel (CSV injection).
 */
export function csvCell(value: string | number | null | undefined): string {
  let text = value === null || value === undefined ? '' : String(value);
  if (/^[=+\-@\t\r]/.test(text)) text = `'${text}`;
  return `"${text.replace(/"/g, '""')}"`;
}

export function csvRow(...cells: (string | number | null | undefined)[]): string {
  return cells.map(csvCell).join(',');
}

/** Joins rows with CRLF per RFC 4180 — Excel on Windows is the most likely consumer. */
export function csvJoin(lines: string[]): string {
  return lines.join('\r\n');
}

/** Today as yyyy-MM-dd in local time, for export file names. */
export function todayYmd(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** Triggers a browser download of `csv` as `fileName`. Generated in memory — nothing is uploaded. */
export function downloadCsv(csv: string, fileName: string) {
  // BOM so Excel opens the file as UTF-8 (em dashes, accented names).
  const blob = new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
