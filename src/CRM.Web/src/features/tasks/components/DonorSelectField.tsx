import { useEffect, useMemo, useRef, useState } from 'react';
import { Search, Check, ChevronDown } from 'lucide-react';
import { useDonors } from '@/features/donors/hooks';

interface DonorSelectFieldProps {
  value: string;
  onChange: (donorId: string, companyName: string) => void;
  error?: string;
  /** Pre-fills the visible label when editing (the id is already in `value`). */
  initialLabel?: string;
}

/** Type-to-search donor picker for the task form. Backs onto GET /api/v1/donors. */
export function DonorSelectField({ value, onChange, error, initialLabel }: DonorSelectFieldProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  // Seeded once from `initialLabel` (edit mode); thereafter driven by selection.
  const [label, setLabel] = useState(initialLabel ?? '');
  const boxRef = useRef<HTMLDivElement>(null);

  const { data, isFetching } = useDonors({ search: search.trim() || undefined, pageSize: 8 });
  const options = useMemo(() => data?.data ?? [], [data]);

  useEffect(() => {
    function onDocClick(event: MouseEvent) {
      if (boxRef.current && !boxRef.current.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, []);

  return (
    <div className="flex flex-col gap-2" ref={boxRef}>
      <span className="text-xs font-bold text-foreground">
        Donor <span aria-hidden className="text-destructive">*</span>
      </span>

      <div className="relative">
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className={`flex h-11 w-full items-center gap-2.5 rounded-2xl border bg-field px-3.75 text-left text-[13.5px] font-medium outline-none focus-visible:ring-2 focus-visible:ring-brand/60 ${
            error ? 'border-destructive/70' : 'border-border'
          }`}
        >
          <Search className="h-3.75 w-3.75 shrink-0 text-muted-foreground" />
          <span className={`flex-1 truncate ${value ? 'text-foreground' : 'text-muted-foreground'}`}>
            {value ? label || 'Selected donor' : 'Search donors…'}
          </span>
          <ChevronDown className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        </button>

        {open && (
          <div className="absolute left-0 right-0 top-12 z-30 rounded-2xl border border-border bg-card p-2 shadow-[0_14px_34px_rgba(20,20,15,0.18)]">
            <div className="mb-1.5 flex h-9 items-center gap-2 rounded-full border border-border bg-field px-3">
              <Search className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
              <input
                autoFocus
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search by company name…"
                className="min-w-0 flex-1 border-none bg-transparent text-[12.5px] font-semibold text-foreground outline-none"
              />
            </div>
            <div className="flex max-h-56 flex-col gap-0.5 overflow-y-auto">
              {options.map((donor) => {
                const selected = donor.id === value;
                return (
                  <button
                    type="button"
                    key={donor.id}
                    onClick={() => {
                      setLabel(donor.companyName);
                      onChange(donor.id, donor.companyName);
                      setOpen(false);
                      setSearch('');
                    }}
                    className={`flex items-center justify-between gap-2 rounded-lg px-3 py-2 text-left text-[12.5px] font-semibold text-foreground transition-colors hover:bg-hover ${
                      selected ? 'bg-hover' : ''
                    }`}
                  >
                    <span className="truncate">{donor.companyName}</span>
                    {selected && <Check className="h-3.5 w-3.5 shrink-0 text-brand" />}
                  </button>
                );
              })}
              {options.length === 0 && (
                <div className="px-3 py-3 text-center text-[12.5px] font-semibold text-muted-foreground">
                  {isFetching ? 'Searching…' : 'No donors match.'}
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {error && <span className="text-xs text-destructive">{error}</span>}
    </div>
  );
}
