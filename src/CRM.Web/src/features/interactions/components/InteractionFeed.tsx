import { useState } from 'react';
import { History, MessageSquare, TriangleAlert } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useInteractions } from '../hooks';
import { InteractionCard } from './InteractionCard';
import { LogInteractionDialog } from './LogInteractionDialog';

interface InteractionFeedProps {
  donorId: string;
}

export function InteractionFeed({ donorId }: InteractionFeedProps) {
  const [logOpen, setLogOpen] = useState(false);
  const { data, isPending, isError, error, refetch, fetchNextPage, hasNextPage, isFetchingNextPage } =
    useInteractions(donorId);

  const interactions = data?.pages.flatMap((page) => page.data) ?? [];
  const totalCount = data?.pages[0]?.pagination.totalCount ?? 0;

  return (
    <section>
      <div className="mb-3.5 flex items-center justify-between gap-3.5">
        <h2 className="m-0 text-[15px] font-extrabold tracking-tight text-[var(--ink)]">Interaction timeline</h2>
        <Button size="sm" onClick={() => setLogOpen(true)}>
          <MessageSquare className="h-3.5 w-3.5" />
          <span>Log Interaction</span>
        </Button>
      </div>

      <div className="rounded-2xl border border-[var(--border)] bg-[var(--card)] px-6 py-1.5 shadow-[0_1px_3px_var(--shadow)]">
        {isPending ? (
          <div className="flex flex-col gap-3 py-6" aria-hidden>
            {Array.from({ length: 3 }).map((_, index) => (
              <div key={index} className="h-16 animate-pulse rounded-xl bg-muted" />
            ))}
          </div>
        ) : isError ? (
          <div className="flex flex-col items-center gap-2 py-12 text-center">
            <TriangleAlert className="h-5 w-5 text-muted-foreground" />
            <p className="m-0 text-sm font-semibold text-foreground">Couldn't load the timeline</p>
            <p className="m-0 text-xs text-muted-foreground">
              {error?.response?.data?.message ?? 'Something went wrong.'}
            </p>
            <button
              type="button"
              onClick={() => refetch()}
              className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
            >
              Retry
            </button>
          </div>
        ) : interactions.length === 0 ? (
          <div className="flex flex-col items-center gap-3 py-12 text-center">
            <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-icon-bg">
              <History className="h-5 w-5 text-icon" />
            </span>
            <p className="m-0 text-sm font-bold tracking-tight text-foreground">No interactions logged yet</p>
            <p className="m-0 max-w-[280px] text-xs font-medium text-muted-foreground">
              Log the first call, email, or meeting with this donor.
            </p>
          </div>
        ) : (
          <>
            {interactions.map((interaction, index) => (
              <InteractionCard
                key={interaction.id}
                interaction={interaction}
                isLast={index === interactions.length - 1 && !hasNextPage}
              />
            ))}
            {hasNextPage && (
              <div className="flex justify-center py-4">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => fetchNextPage()}
                  disabled={isFetchingNextPage}
                >
                  {isFetchingNextPage ? 'Loading…' : `Load older (${interactions.length} of ${totalCount})`}
                </Button>
              </div>
            )}
          </>
        )}
      </div>

      <LogInteractionDialog donorId={donorId} open={logOpen} onOpenChange={setLogOpen} />
    </section>
  );
}
