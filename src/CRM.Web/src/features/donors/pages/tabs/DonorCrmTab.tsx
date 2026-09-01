import { Check, Clock, Database } from 'lucide-react';
import type { DonorDetailDto } from '../../types';
import { getInitials, getAvatarColor } from '../../lib/avatar';
import { formatDate } from '../../lib/donorFormatters';

export function DonorCrmTab({ donor }: { donor: DonorDetailDto }) {
  const { crm } = donor;
  const rmName = crm.relationshipManager?.fullName;

  return (
    <section className="bg-[var(--card)] border border-[var(--border)] rounded-2xl shadow-[0_1px_3px_var(--shadow)] p-6.5">
      <h2 className="m-0 mb-5 text-15 font-extrabold tracking-tight text-[var(--ink)]">
        Relationship management
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            RELATIONSHIP MANAGER
          </div>
          {rmName ? (
            <div className="flex items-center gap-2.5">
              <span
                className="w-8 h-8 rounded-full text-white text-xs font-bold flex items-center justify-center"
                style={{ backgroundColor: getAvatarColor(rmName) }}
              >
                {getInitials(rmName)}
              </span>
              <div>
                <div className="text-[13.5px] font-bold text-[var(--ink)]">{rmName}</div>
                <div className="text-[11.5px] font-medium text-[var(--muted2)]">
                  Primary RM
                </div>
              </div>
            </div>
          ) : (
            <div className="text-[13.5px] font-medium text-[var(--muted-c)]">Unassigned</div>
          )}
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            MARKETING CONSENT
          </div>
          <div className="flex items-center gap-2.5 flex-wrap">
            {crm.marketingConsent ? (
              <>
                <span className="inline-flex items-center gap-1.75 px-3 py-1.5 rounded-2xl bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 text-[12.5px] font-bold">
                  <Check className="w-3.25 h-3.25 stroke-[2.8]" />
                  <span>Granted</span>
                </span>
                <span className="text-xs font-medium text-[var(--muted-c)]">
                  {formatDate(crm.marketingConsentDate) || 'No date recorded'}
                </span>
              </>
            ) : (
              <span className="inline-flex items-center gap-1.75 px-3 py-1.5 rounded-2xl bg-[var(--card)] border border-[var(--border)] text-[var(--muted-c)] text-[12.5px] font-bold">
                <span>Not given</span>
              </span>
            )}
          </div>
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            FOLLOW-UP DATE
          </div>
          {crm.followUpDate ? (
            <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-2xl bg-[var(--field)] border border-[var(--border)] text-[var(--ink)] text-[12.5px] font-bold">
              <Clock className="w-3.25 h-3.25" />
              <span>{formatDate(crm.followUpDate)}</span>
            </div>
          ) : (
            <div className="text-[13.5px] font-medium text-[var(--muted-c)]">—</div>
          )}
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            IMPACT REPORTING PREFERENCES
          </div>
          <div className="text-[13.5px] font-medium text-[var(--ink)]">
            {crm.impactReportingPreferences || 'None specified'}
          </div>
        </div>

        <div>
          <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
            FOODSPACE COMPANY ID
          </div>
          {donor.foodspaceCompanyId ? (
            <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-2xl bg-[var(--field)] border border-solid border-[var(--border)] text-[12.5px] font-semibold text-[var(--ink)]">
              <Database className="w-3.25 h-3.25" />
              <span>{donor.foodspaceCompanyId}</span>
            </div>
          ) : (
            <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-2xl bg-[var(--field)] border border-dashed border-[var(--border)] text-[12.5px] font-semibold text-[var(--muted2)]">
              <Database className="w-3.25 h-3.25" />
              <span>Not yet synced</span>
            </div>
          )}
        </div>
      </div>

      <div className="h-px bg-[var(--divider)] my-6" />
      <div className="text-[11px] font-bold tracking-wider text-[var(--muted2)] mb-2.25 uppercase">
        ADDITIONAL INFORMATION
      </div>
      <p className="m-0 max-w-[760px] text-[13.5px] font-medium leading-relaxed text-[var(--ink)] whitespace-pre-line">
        {crm.additionalInformation || 'No notes available.'}
      </p>
    </section>
  );
}
