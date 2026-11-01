import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bookmark, Trash2 } from 'lucide-react';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { paths } from '@/routes/paths';
import { useDeleteSavedFilter, useSavedFilters } from '../hooks';

interface SavedFiltersMenuProps {
  /** Classes for the trigger, so it matches the other rail buttons. */
  triggerClassName: string;
}

/** Rail action (Issue 103): the user's saved donor filters. Opening one applies it to the donor list. */
export function SavedFiltersMenu({ triggerClassName }: SavedFiltersMenuProps) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const filters = useSavedFilters(open);
  const deleteFilter = useDeleteSavedFilter();

  const onOpenChange = (next: boolean) => {
    setOpen(next);
    if (!next) setConfirmingId(null);
  };

  return (
    <Popover open={open} onOpenChange={onOpenChange}>
      <PopoverTrigger
        render={
          <button type="button" title="Saved filters" aria-label="Saved filters" className={triggerClassName}>
            <Bookmark className="h-4 w-4" />
          </button>
        }
      />
      <PopoverContent side="right" align="start" className="w-72 gap-2 p-3">
        <p className="m-0 px-1 text-[12.5px] font-bold text-foreground">Saved filters</p>

        {filters.isLoading && <p className="m-0 px-1 text-[12.5px] text-muted-foreground">Loading…</p>}
        {filters.isError && <p className="m-0 px-1 text-[12.5px] text-destructive">Couldn't load your saved filters.</p>}
        {filters.isSuccess && filters.data.length === 0 && (
          <p className="m-0 px-1 text-[12.5px] text-muted-foreground">
            No saved filters yet. Filter the donor list and click Save filters.
          </p>
        )}

        <ul className="m-0 flex list-none flex-col gap-0.5 p-0">
          {filters.data?.map((filter) => (
            <li key={filter.id} className="flex items-center gap-1 rounded-xl hover:bg-hover">
              {confirmingId === filter.id ? (
                <div className="flex w-full items-center justify-between gap-2 px-2.5 py-1.5 text-[12.5px]">
                  <span className="truncate">Delete “{filter.name}”?</span>
                  <span className="flex shrink-0 gap-1">
                    <button
                      type="button"
                      onClick={() => deleteFilter.mutate(filter.id, { onSettled: () => setConfirmingId(null) })}
                      disabled={deleteFilter.isPending}
                      className="rounded-full bg-destructive px-2.5 py-1 font-bold text-white"
                    >
                      Delete
                    </button>
                    <button type="button" onClick={() => setConfirmingId(null)} className="rounded-full border border-border px-2.5 py-1 font-bold">
                      Cancel
                    </button>
                  </span>
                </div>
              ) : (
                <>
                  <button
                    type="button"
                    onClick={() => {
                      onOpenChange(false);
                      navigate(`${paths.donors}?${filter.queryString}`);
                    }}
                    className="min-w-0 flex-1 truncate px-2.5 py-2 text-left text-[13px] font-semibold text-foreground"
                  >
                    {filter.name}
                  </button>
                  <button
                    type="button"
                    aria-label={`Delete saved filter ${filter.name}`}
                    onClick={() => setConfirmingId(filter.id)}
                    className="mr-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-icon hover:text-destructive"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </>
              )}
            </li>
          ))}
        </ul>
        {deleteFilter.isError && <p className="m-0 px-1 text-[12.5px] text-destructive">Couldn't delete that filter.</p>}
      </PopoverContent>
    </Popover>
  );
}
