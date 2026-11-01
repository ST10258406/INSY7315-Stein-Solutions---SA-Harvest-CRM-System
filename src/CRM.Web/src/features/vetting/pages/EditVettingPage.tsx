import { Link, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { paths } from '@/routes/paths';
import { VettingForm } from '../components/VettingForm';

export default function EditVettingPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <div className="flex flex-col gap-6 p-8">
      <div>
        <Link to={paths.vettingDetail(id!)} className="flex w-fit items-center gap-1.5 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground">
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to submission
        </Link>
        <h1 className="mt-3 text-2xl font-semibold text-foreground">Edit vetting</h1>
      </div>
      <VettingForm mode="edit" submissionId={id} />
    </div>
  );
}
