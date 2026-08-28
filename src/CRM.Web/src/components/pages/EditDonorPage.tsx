import { Link, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { paths } from '@/routes/paths';
import { DonorForm } from '@/features/donors/components/DonorForm';

export default function EditDonorPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <div className="flex flex-col gap-6 p-8">
      <div>
        <Link
          to={id ? paths.donorDetail(id) : paths.donors}
          className="flex w-fit items-center gap-1.5 text-xs font-medium text-[#B9B9AE] transition-colors hover:text-[#F4F4EE]"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to donor
        </Link>
        <h1 className="mt-3 text-2xl font-semibold text-[#F4F4EE]">Edit donor</h1>
      </div>

      <DonorForm mode="edit" donorId={id} />
    </div>
  );
}
