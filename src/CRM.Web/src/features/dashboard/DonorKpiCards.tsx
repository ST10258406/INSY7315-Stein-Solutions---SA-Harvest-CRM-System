import { Users, UserCheck, Clock, UserX } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useDonors } from '@/features/donors/hooks';

/**
 * No dedicated aggregate/count endpoint exists yet (tracked as a backend
 * follow-up — see Issue 39 notes). Until one lands, each card asks GetDonors
 * for pageSize=1 under the relevant status filter and reads
 * `pagination.totalCount`, which the backend computes via a SQL COUNT
 * (DonorRepository.SearchAsync) rather than materializing the full donor
 * list — so this stays within the lightweight-DTO performance decision from
 * Issue 30/63 instead of counting client-side.
 */
export function DonorKpiCards() {
  const total = useDonors({ pageSize: 1 });
  const active = useDonors({ status: 'Active', pageSize: 1 });
  const pending = useDonors({ status: 'PendingReview', pageSize: 1 });
  const lapsed = useDonors({ status: 'Lapsed', pageSize: 1 });

  const cards = [
    { label: 'Total donors', icon: Users, query: total },
    { label: 'Active donors', icon: UserCheck, query: active },
    { label: 'Pending review', icon: Clock, query: pending },
    { label: 'Lapsed', icon: UserX, query: lapsed },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {cards.map((card) => (
        <KpiCard key={card.label} {...card} />
      ))}
    </div>
  );
}

interface KpiCardProps {
  label: string;
  icon: LucideIcon;
  query: ReturnType<typeof useDonors>;
}

function KpiCard({ label, icon: Icon, query }: KpiCardProps) {
  const { data, isPending, isError } = query;

  return (
    <div className="rounded-2xl border border-border bg-card p-5">
      <div className="flex items-center justify-between">
        <span className="text-xs font-medium tracking-wide text-muted-foreground uppercase">{label}</span>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </div>

      <div className="mt-3">
        {isPending ? (
          <div className="h-9 w-16 animate-pulse rounded-md bg-muted" aria-hidden />
        ) : isError ? (
          <span className="text-2xl font-semibold text-muted-foreground" title="Couldn't load this count">
            —
          </span>
        ) : (
          <span className="text-3xl font-semibold text-foreground">{data.pagination.totalCount}</span>
        )}
      </div>
    </div>
  );
}
