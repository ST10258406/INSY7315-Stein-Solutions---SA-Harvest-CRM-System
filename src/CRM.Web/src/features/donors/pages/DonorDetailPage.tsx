import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Pencil, SearchX, TriangleAlert } from 'lucide-react';
import { paths } from '@/routes/paths';
import { useDonor } from '../hooks';
import { DonorStatusBadge } from '../components/DonorStatusBadge';
import { DonorTabs } from './DonorTabs';

export default function DonorDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: donor, isPending, isError, error, refetch } = useDonor(id);

  if (isPending) {
    return (
      <div className="flex flex-col gap-6 p-8">
        <div className="h-8 w-64 animate-pulse rounded-md bg-muted" aria-hidden />
        <div className="h-48 animate-pulse rounded-2xl bg-muted" aria-hidden />
      </div>
    );
  }

  if (isError) {
    const isNotFound = error?.response?.status === 404;

    return (
      <div className="flex flex-col items-center justify-center gap-3 p-16 text-center">
        {isNotFound ? (
          <SearchX className="h-6 w-6 text-muted-foreground" />
        ) : (
          <TriangleAlert className="h-6 w-6 text-muted-foreground" />
        )}
        <p className="text-sm font-medium text-foreground">
          {isNotFound ? "This donor doesn't exist" : "Couldn't load this donor"}
        </p>
        <p className="text-xs text-muted-foreground">
          {isNotFound
            ? "It may have been removed, or the link is incorrect."
            : (error?.response?.data?.message ?? 'Something went wrong.')}
        </p>
        {isNotFound ? (
          <Link
            to={paths.donors}
            className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
          >
            Back to donors
          </Link>
        ) : (
          <button
            type="button"
            onClick={() => refetch()}
            className="mt-1 rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:border-foreground/60"
          >
            Retry
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6 p-8">
      <div>
        <Link
          to={paths.donors}
          className="flex w-fit items-center gap-1.5 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to donors
        </Link>

        <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-semibold text-foreground">{donor.company.companyName}</h1>
            <DonorStatusBadge status={donor.status} />
          </div>

          <Link
            to={paths.donorEdit(donor.id)}
            className="flex h-9 items-center gap-1.5 rounded-full bg-brand px-4 text-[13px] font-bold text-primary-foreground transition-opacity hover:opacity-90"
          >
            <Pencil className="h-4 w-4" />
            Edit
          </Link>
        </div>
      </div>

      <DonorTabs donor={donor} />
    </div>
  );
}
