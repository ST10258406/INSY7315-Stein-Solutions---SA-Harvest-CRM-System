import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Pencil, SearchX, TriangleAlert, Building2, FileText } from 'lucide-react';
import { paths } from '@/routes/paths';
import { LogInteractionDialog, EmailPanel } from '@/features/interactions';
import { useDonor } from '../hooks';
import { DonorStatusBadge } from '../components/DonorStatusBadge';
import { getInitials, getAvatarColor } from '../lib/avatar';
import { DonorTabs } from './DonorTabs';
import { Button, buttonVariants } from '@/components/ui/button';

export default function DonorDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: donor, isPending, isError, error, refetch } = useDonor(id);
  const [logOpen, setLogOpen] = useState(false);
  const [composeEmailOpen, setComposeEmailOpen] = useState(false);

  if (isPending) {
    return (
      <div className="flex flex-col gap-6 p-[26px_30px_34px]">
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

  const rmName = donor.crm.relationshipManager?.fullName ?? null;

  return (
    <div className="p-[22px_30px_40px]">
      <DonorTabs donor={donor}>
        <div className="mb-5 flex items-start gap-6 flex-wrap">
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-3 flex-wrap">
              <h1 className="m-0 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">
                Donors <span className="text-[var(--muted2)] font-semibold">—</span> {donor.company.companyName}
              </h1>
              <DonorStatusBadge status={donor.status} />
            </div>
            <p className="m-0 mt-1.5 text-13 text-[var(--muted-c)] font-medium">
              Trading as {donor.company.tradingName || donor.company.companyName}
            </p>

            <div className="mt-4 flex flex-wrap items-center gap-2.5">
              <div className="flex h-8.5 items-center gap-2 rounded-full border border-[var(--border)] bg-[var(--card)] px-3.25 text-[12.5px] font-semibold text-[var(--ink)]">
                <Building2 className="h-3.75 w-3.75 text-[var(--icon)]" />
                <span>{donor.company.companyType.name}</span>
              </div>

              <div className="flex h-8.5 items-center gap-2 rounded-full border border-[var(--border)] bg-[var(--card)] px-3.25 text-[12.5px] font-semibold text-[var(--ink)]">
                <FileText className="h-3.75 w-3.75 text-[var(--icon)]" />
                <span>{donor.submissionSource === 'Internal' ? 'Internal Form' : 'Public Form'}</span>
              </div>

              {rmName && (
                <div className="flex h-8.5 items-center gap-2.25 rounded-full border border-[var(--border)] bg-[var(--card)] py-1.25 pr-3.25 pl-1.25 text-[12.5px] font-semibold text-[var(--ink)]">
                  <span
                    className="flex h-6 w-6 items-center justify-center rounded-full text-[9.5px] font-bold text-white"
                    style={{ backgroundColor: getAvatarColor(rmName) }}
                  >
                    {getInitials(rmName)}
                  </span>
                  <span>{rmName}</span>
                  <span className="text-[11.5px] font-semibold text-[var(--muted2)]">RM</span>
                </div>
              )}
            </div>
          </div>

          <div className="ml-auto flex items-center gap-2.5">
            <Link
              to={paths.donors}
              className={buttonVariants({ variant: 'secondary', size: 'sm' })}
            >
              <ArrowLeft className="w-3.75 h-3.75 stroke-[2.2]" />
              <span>Back</span>
            </Link>

            <Link
              to={paths.donorEdit(donor.id)}
              className={buttonVariants({ variant: 'secondary', size: 'sm' })}
            >
              <Pencil className="w-3.75 h-3.75 text-[var(--ink)]" />
              <span>Edit</span>
            </Link>

            <Button variant="secondary" size="sm" onClick={() => setComposeEmailOpen(true)}>
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-mail w-3.75 h-3.75 text-[var(--ink)]"><rect width="20" height="16" x="2" y="4" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/></svg>
              <span>Send Email</span>
            </Button>

            <Button variant="default" size="sm" onClick={() => setLogOpen(true)}>
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="lucide lucide-message-square w-4 h-4"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
              <span>Log Interaction</span>
            </Button>
          </div>
        </div>
      </DonorTabs>

      <LogInteractionDialog donorId={donor.id} open={logOpen} onOpenChange={setLogOpen} />
      <EmailPanel
        donorId={donor.id}
        donorName={donor.company.companyName}
        contactName={donor.primaryContact.name}
        defaultTo={donor.primaryContact.email ?? ''}
        open={composeEmailOpen}
        onOpenChange={setComposeEmailOpen}
      />
    </div>
  );
}
