import { ShieldAlert, FileText, Download } from 'lucide-react';
import type { DonorDetailDto } from '../../types';
import { Button } from '@/components/ui/button';

export function DonorLegalTab({ donor }: { donor: DonorDetailDto }) {
  const { company, legalAddress, compliance } = donor;

  const formattedAddress = legalAddress 
    ? [legalAddress.streetNameNumber, legalAddress.suburb, legalAddress.city, legalAddress.province.name, legalAddress.postalCode].filter(Boolean).join('\n')
    : 'No legal address on file.';

  return (
    <section className="grid grid-cols-1 lg:grid-cols-[minmax(0,1.45fr)_minmax(0,1fr)] gap-4.5 items-start">
      {/* Company Details */}
      <div className="bg-[var(--card)] border border-[var(--border)] rounded-2xl shadow-[0_1px_3px_var(--shadow)] p-6.5">
        <h2 className="m-0 mb-5 text-15 font-extrabold tracking-tight text-[var(--ink)]">
          Legal & registration
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
              REGISTERED COMPANY NAME
            </div>
            <div className="text-sm font-semibold text-[var(--ink)]">
              {company.registeredCompanyName || '—'}
            </div>
          </div>
          <div>
            <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
              LEGAL ENTITY TYPE
            </div>
            <div className="text-sm font-semibold text-[var(--ink)]">
              {company.entityType.name || '—'}
            </div>
          </div>
          <div>
            <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
              COMPANY REGISTRATION NUMBER
            </div>
            <div className="text-sm font-semibold text-[var(--ink)] tabular-nums">
              {company.companyRegistrationNumber || '—'}
            </div>
          </div>
          <div>
            <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-1.75 uppercase">
              INCOME TAX NUMBER
            </div>
            <div className="text-sm font-semibold text-[var(--ink)] tabular-nums">
              {company.incomeTaxNumber || '—'}
            </div>
          </div>
        </div>
        <div className="h-px bg-[var(--divider)] my-6" />
        <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
          REGISTERED ADDRESS
        </div>
        <div className="text-[13.5px] font-medium leading-relaxed text-[var(--ink)] whitespace-pre-line">
          {formattedAddress}
        </div>
      </div>

      {/* B-BBEE Card (Admin Only) */}
      <div className="bg-[var(--card)] border border-[var(--border)] rounded-2xl shadow-[0_1px_3px_var(--shadow)] p-6.5">
        <div className="flex items-center gap-2.5 mb-1.5">
          <h2 className="m-0 text-15 font-extrabold tracking-tight text-[var(--ink)]">
            B-BBEE
          </h2>
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-[var(--icon-bg)] text-[10.5px] font-bold tracking-wide text-[var(--muted-c)]">
            <ShieldAlert className="w-3 h-3 text-[var(--muted-c)]" />
            <span>ADMIN ONLY</span>
          </span>
        </div>
        <p className="m-0 mb-5 text-xs font-medium text-[var(--muted2)] leading-relaxed">
          Procurement and Marketing roles do not see this card.
        </p>

        <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2 uppercase">
          B-BBEE STATUS
        </div>
        {compliance.bbbeeStatus ? (
          <span className="inline-flex items-center gap-2 px-3.5 py-1.75 rounded-2xl bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 text-[12.5px] font-bold">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
            <span>{compliance.bbbeeStatus.name}</span>
          </span>
        ) : (
          <span className="inline-flex items-center gap-2 px-3.5 py-1.75 rounded-2xl bg-[var(--card)] border border-[var(--border)] text-[var(--muted-c)] text-[12.5px] font-bold">
            <span className="w-1.5 h-1.5 rounded-full bg-[var(--muted2)]" />
            <span>Not verified</span>
          </span>
        )}

        <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mt-6 mb-2.25 uppercase">
          B-BBEE DOCUMENTS
        </div>
        
        {compliance.documents.length > 0 ? (
          <div className="flex flex-col gap-2.5">
            {compliance.documents.map((doc) => (
              <div key={doc.id} className="flex items-center gap-3 p-3.5 rounded-xl bg-[var(--field)] border border-[var(--border)]">
                <div className="w-9 h-9 rounded-lg bg-[var(--card)] border border-[var(--border)] flex items-center justify-center shrink-0">
                  <FileText className="w-4.25 h-4.25 text-[var(--icon)]" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="text-13 font-bold text-[var(--ink)] truncate">
                    {doc.fileName}
                  </div>
                  <div className="text-[11.5px] font-medium text-[var(--muted2)] mt-0.5">
                    {doc.documentType.name}
                  </div>
                </div>
                <Button variant="secondary" size="sm" className="shrink-0">
                  <Download className="w-3.5 h-3.5 stroke-[1.9]" />
                  <span>Download</span>
                </Button>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-[13.5px] font-medium text-[var(--muted2)]">
            No B-BBEE documents uploaded yet.
          </div>
        )}
      </div>
    </section>
  );
}
