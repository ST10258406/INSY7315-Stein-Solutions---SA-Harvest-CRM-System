import { useEffect, useState, type KeyboardEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';
import { donorKeys, fetchDonors } from '@/features/donors/hooks';
import { DonorStatusBadge } from '@/features/donors/components/DonorStatusBadge';
import { ADMIN_ROLES, NAV_ITEMS } from '@/features/shell/navItems';

// Matches the ProcurementOrAbove policy on GET /api/v1/donors (cosmetic; the API enforces it).
const DONOR_SEARCH_ROLES = ['Procurement', 'Admin', 'SuperAdmin'];
const MIN_CHARS = 2;
const DEBOUNCE_MS = 300;
const DONOR_LIMIT = 5;

interface QuickSearchDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

interface Result {
  id: string;
  label: string;
  hint?: string;
  status?: string;
  to: string;
}

/** Issue 102: jump to a donor by company name, or to any page the user's role can open. */
export function QuickSearchDialog({ open, onOpenChange }: QuickSearchDialogProps) {
  const navigate = useNavigate();
  const roles = useAuthStore((state) => state.user?.roles ?? []);
  const canSearchDonors = roles.some((role) => DONOR_SEARCH_ROLES.includes(role));
  const isAdmin = roles.some((role) => ADMIN_ROLES.includes(role));

  const [input, setInput] = useState('');
  const [term, setTerm] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    const timeout = setTimeout(() => setTerm(input.trim()), DEBOUNCE_MS);
    return () => clearTimeout(timeout);
  }, [input]);

  const filters = { search: term, pageSize: DONOR_LIMIT };
  const donors = useQuery({
    queryKey: donorKeys.list(filters),
    queryFn: () => fetchDonors(filters),
    enabled: open && canSearchDonors && term.length >= MIN_CHARS,
  });

  const typed = input.trim().toLowerCase();
  const pageResults: Result[] = NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin)
    .filter((item) => item.label.toLowerCase().includes(typed))
    .map((item) => ({ id: `page-${item.path}`, label: item.label, hint: 'Page', to: item.path }));

  const showDonors = canSearchDonors && term.length >= MIN_CHARS;
  const donorResults: Result[] = showDonors
    ? (donors.data?.data ?? []).map((d) => ({
        id: `donor-${d.id}`,
        label: d.companyName,
        status: d.status,
        to: paths.donorDetail(d.id),
      }))
    : [];
  const seeAll: Result[] = showDonors
    ? [{ id: 'see-all', label: `See all donor results for “${term}”`, to: `${paths.donors}?search=${encodeURIComponent(term)}` }]
    : [];

  const results = [...donorResults, ...seeAll, ...pageResults];
  const active = Math.min(activeIndex, Math.max(results.length - 1, 0));

  const close = (next: boolean) => {
    if (!next) {
      setInput('');
      setTerm('');
      setActiveIndex(0);
    }
    onOpenChange(next);
  };

  const go = (result: Result) => {
    close(false);
    navigate(result.to);
  };

  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault();
      if (results.length === 0) return;
      const step = e.key === 'ArrowDown' ? 1 : -1;
      setActiveIndex((active + step + results.length) % results.length);
    } else if (e.key === 'Enter' && results[active]) {
      e.preventDefault();
      go(results[active]);
    }
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="w-[560px] max-w-[calc(100vw-2rem)]">
        <DialogTitle>Quick search</DialogTitle>
        <DialogDescription>
          {canSearchDonors ? 'Find a donor by company name, or jump to a page.' : 'Jump to a page.'}
        </DialogDescription>

        <div className="relative mt-4">
          <Search className="pointer-events-none absolute top-1/2 left-4 h-4 w-4 -translate-y-1/2 text-[var(--icon)]" />
          <input
            autoFocus
            type="text"
            role="combobox"
            aria-label="Search"
            aria-expanded={results.length > 0}
            aria-controls="quick-search-results"
            aria-activedescendant={results[active] ? `quick-search-${results[active].id}` : undefined}
            value={input}
            onChange={(e) => {
              setInput(e.target.value);
              setActiveIndex(0);
            }}
            onKeyDown={onKeyDown}
            placeholder={canSearchDonors ? 'Company name or page…' : 'Page name…'}
            className="w-full rounded-full border border-border bg-field py-2.5 pr-4 pl-11 text-[13.5px] text-foreground outline-none focus:border-foreground"
          />
        </div>

        <div className="mt-3 min-h-[2rem] text-[12.5px] text-muted-foreground" aria-live="polite">
          {showDonors && donors.isFetching && 'Searching donors…'}
          {showDonors && donors.isError && <span className="text-destructive">Couldn't search donors. Pages are still listed.</span>}
          {showDonors && donors.isSuccess && donorResults.length === 0 && `No donors match “${term}”.`}
          {canSearchDonors && input.trim().length > 0 && input.trim().length < MIN_CHARS && `Type at least ${MIN_CHARS} characters to search donors.`}
        </div>

        <ul id="quick-search-results" role="listbox" aria-label="Search results" className="m-0 flex max-h-[360px] list-none flex-col gap-0.5 overflow-y-auto p-0">
          {results.map((result, index) => (
            <li
              key={result.id}
              id={`quick-search-${result.id}`}
              role="option"
              aria-selected={index === active}
              onMouseEnter={() => setActiveIndex(index)}
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => go(result)}
              className={`flex cursor-pointer items-center justify-between gap-3 rounded-xl px-3.5 py-2.5 text-[13px] text-foreground ${
                index === active ? 'bg-hover' : ''
              }`}
            >
              <span className="truncate font-semibold">{result.label}</span>
              {result.status ? (
                <DonorStatusBadge status={result.status} />
              ) : (
                result.hint && <span className="shrink-0 text-[11.5px] text-muted-foreground">{result.hint}</span>
              )}
            </li>
          ))}
        </ul>
        {results.length === 0 && <p className="m-0 text-[12.5px] text-muted-foreground">No matches.</p>}
      </DialogContent>
    </Dialog>
  );
}
