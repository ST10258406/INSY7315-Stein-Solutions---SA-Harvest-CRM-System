import { ListTodo, CheckCircle2 } from 'lucide-react';
import { DonorKpiCards } from './DonorKpiCards';
import { ComingSoonPanel } from './ComingSoonPanel';

export default function DashboardPage() {
  return (
    <div className="flex flex-col gap-8 p-8">
      <div>
        <h1 className="text-2xl font-semibold text-[#F4F4EE]">Dashboard</h1>
        <p className="mt-1 text-sm text-[#B9B9AE]">An overview of donor activity across S.A. Harvest.</p>
      </div>

      <DonorKpiCards />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ComingSoonPanel
          icon={ListTodo}
          title="Overdue follow-ups"
          description="Coming in a future update."
        />
        <ComingSoonPanel
          icon={CheckCircle2}
          title="Pending approvals"
          description="Coming in a future update."
        />
      </div>
    </div>
  );
}
