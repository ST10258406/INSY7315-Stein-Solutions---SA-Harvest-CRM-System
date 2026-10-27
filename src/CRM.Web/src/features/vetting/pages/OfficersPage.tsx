import { useState } from 'react';
import { Pencil, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { getAvatarColor } from '@/features/donors/lib/avatar';
import { useOfficers } from '../hooks';
import { OfficerFormDialog } from '../components/OfficerFormDialog';
import { TD, TH, VettingTableStates } from '../components/VettingTableStates';
import type { VettingOfficer } from '../types';

/** /vetting/officers — Admin only (route-guarded; the API enforces AdminOrAbove on writes). */
export default function OfficersPage() {
  const { data: officers = [], isPending, isError, error, refetch } = useOfficers();
  const [dialog, setDialog] = useState<{ officer?: VettingOfficer } | null>(null);

  return (
    <main className="min-w-0 flex-1 overflow-y-auto p-[26px_30px_40px]">
      <div className="mb-5 flex flex-wrap items-end gap-6">
        <div>
          <h1 className="m-0 mb-1.5 text-[30px] font-extrabold tracking-tight text-[var(--ink)]">Vetting officers</h1>
          <p className="m-0 text-sm font-medium text-[var(--muted-c)]">The field officers who carry out vetting visits.</p>
        </div>
        <div className="ml-auto">
          <Button size="sm" onClick={() => setDialog({})}>
            <Plus className="h-4 w-4 stroke-[2.2]" />
            <span>New officer</span>
          </Button>
        </div>
      </div>

      <div className="overflow-x-auto rounded-xl bg-[var(--card)] shadow-[0_1px_3px_var(--shadow)]">
        <table className="w-full min-w-[640px] border-collapse">
          <thead>
            <tr className="bg-[var(--hair)]">
              <th className={TH}>Initials</th>
              <th className={TH}>Full name</th>
              <th className={TH}>Assigned region</th>
              <th className={TH}>Workload</th>
              <th className={TH}>
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>
          <tbody>
            <VettingTableStates
              colSpan={5}
              isPending={isPending}
              isError={isError}
              error={error}
              onRetry={refetch}
              isEmpty={officers.length === 0}
              emptyText="No officers yet."
            />
            {officers.map((officer) => (
              <tr key={officer.id}>
                <td className={TD}>
                  <span
                    className="inline-flex h-8 min-w-8 items-center justify-center rounded-full px-1.5 text-[11px] font-bold text-white"
                    style={{ backgroundColor: getAvatarColor(officer.fullName) }}
                  >
                    {officer.initials}
                  </span>
                </td>
                <td className={TD}>{officer.fullName}</td>
                <td className={TD}>{officer.assignedRegion}</td>
                <td className={TD} title="Open site visits plus open submissions">
                  {officer.workloadCount}
                </td>
                <td className={`${TD} text-right`}>
                  <Button variant="ghost" size="sm" onClick={() => setDialog({ officer })} aria-label={`Edit ${officer.fullName}`}>
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {dialog && (
        <OfficerFormDialog
          key={dialog.officer?.id ?? 'new'}
          open
          onOpenChange={(open) => !open && setDialog(null)}
          officer={dialog.officer}
        />
      )}
    </main>
  );
}
