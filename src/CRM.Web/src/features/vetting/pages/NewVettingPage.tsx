import { Link, useSearchParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { paths } from '@/routes/paths';
import { VettingForm } from '../components/VettingForm';

/** /vetting/new?donorId=&officerId=&siteVisitId= — the query string pre-fills the header. */
export default function NewVettingPage() {
  const [params] = useSearchParams();
  const prefill = {
    donorId: params.get('donorId') ?? undefined,
    officerId: params.get('officerId') ?? undefined,
    siteVisitId: params.get('siteVisitId') ?? undefined,
  };

  return (
    <div className="flex flex-col gap-6 p-8">
      <div>
        <Link to={paths.vetting} className="flex w-fit items-center gap-1.5 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground">
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to vetting
        </Link>
        <h1 className="mt-3 text-2xl font-semibold text-foreground">New vetting</h1>
      </div>
      <VettingForm mode="create" prefill={prefill} />
    </div>
  );
}
