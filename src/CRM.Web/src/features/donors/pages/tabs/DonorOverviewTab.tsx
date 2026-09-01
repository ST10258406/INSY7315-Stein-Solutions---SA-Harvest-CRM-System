import { ExternalLink } from 'lucide-react';
import type { DonorDetailDto } from '../../types';

export function DonorOverviewTab({ donor }: { donor: DonorDetailDto }) {
  const { company, donations } = donor;
  
  const safeWebsite = (() => {
    if (!company.website) return null;
    try {
      const url = new URL(company.website);
      return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
    } catch {
      return null;
    }
  })();

  const displayWebsite = safeWebsite ? safeWebsite.replace(/^https?:\/\//, '') : '';

  return (
    <section className="bg-[var(--card)] border border-[var(--border)] rounded-2xl shadow-[0_1px_3px_var(--shadow)] p-6.5">
      <h2 className="m-0 mb-5 text-15 font-extrabold tracking-tight text-[var(--ink)]">
        Company & donation profile
      </h2>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
            Company type
          </div>
          <div className="text-sm font-semibold text-[var(--ink)]">{company.companyType.name}</div>
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
            Website
          </div>
          {safeWebsite ? (
            <a
              href={safeWebsite}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1.5 text-sm font-semibold text-[var(--ink)] border-b-[1.5px] border-brand hover:text-brand"
            >
              <span>{displayWebsite}</span>
              <ExternalLink className="w-3.25 h-3.25" />
            </a>
          ) : (
            <div className="text-sm font-semibold text-[var(--muted2)]">—</div>
          )}
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
            Donation frequency
          </div>
          <div className="text-sm font-semibold text-[var(--ink)]">{donations.frequency.name}</div>
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
            Operational regions
          </div>
          <div className="flex items-center gap-1.5 flex-wrap">
            {donations.operationalRegions.map((reg) => (
              <span
                key={reg.id}
                className="px-2.5 py-1 rounded-lg bg-[var(--chip)] text-[11.5px] font-bold text-[var(--ink)]"
              >
                {reg.name}
              </span>
            ))}
          </div>
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
            Donation types
          </div>
          <div className="flex items-center gap-1.5 flex-wrap">
            {donations.types.map((dt) => (
              <span
                key={dt.id}
                className="px-2.5 py-1 rounded-lg bg-[var(--chip)] text-[11.5px] font-bold text-[var(--ink)]"
              >
                {dt.name}
              </span>
            ))}
          </div>
        </div>
      </div>

      <div className="h-px bg-[var(--divider)] my-6" />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            Collection / pickup address
          </div>
          <div className="text-[13.5px] font-medium leading-relaxed text-[var(--ink)] whitespace-pre-line">
            {donations.collectionAddress || '—'}
          </div>
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            Operations / logistics details
          </div>
          <p className="m-0 text-[13.5px] font-medium leading-relaxed text-[var(--ink)] whitespace-pre-line">
            {donations.operationsLogisticsDetails || '—'}
          </p>
        </div>
      </div>
    </section>
  );
}
