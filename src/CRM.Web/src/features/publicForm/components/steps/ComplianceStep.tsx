import type { FieldErrors, UseFormRegister } from 'react-hook-form';
import { Upload } from 'lucide-react';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import { usePublicBbbeeStatuses } from '../../hooks/usePublicLookups';
import { SelectField } from '../fields/SelectField';
import { TextAreaField } from '../fields/TextAreaField';

interface ComplianceStepProps {
  register: UseFormRegister<DonorFormValues>;
  errors: FieldErrors<DonorFormValues>;
}

export function ComplianceStep({ register, errors }: ComplianceStepProps) {
  const bbbeeStatuses = usePublicBbbeeStatuses();

  return (
    <div className="flex flex-col gap-5.5">
      <p className="-mt-1 text-[13px] font-medium text-[#82827A]">All fields on this step are optional.</p>

      <div className="max-w-[340px]">
        <SelectField
          label="BBBEE Status"
          optional
          placeholder="Select a status"
          registration={register('bbbeeStatusId')}
          options={(bbbeeStatuses.data ?? []).map((s) => ({ value: String(s.id), label: s.name }))}
          isLoading={bbbeeStatuses.isPending}
          error={errors.bbbeeStatusId?.message}
        />
      </div>

      <div className="flex flex-col gap-2">
        <span className="text-[12.5px] font-bold text-[#16160F]">
          BBBEE Certificate <span className="font-medium text-[#9A9A90]">(optional)</span>
        </span>
        <div className="flex flex-col items-center gap-2.5 rounded-[14px] border-2 border-dashed border-[#E4E4DE] bg-white p-7.5 text-center">
          <div className="flex h-11 w-11 items-center justify-center rounded-full bg-[#FBF6D4]">
            <Upload className="h-4.5 w-4.5 text-[#8A7A00]" strokeWidth={1.8} />
          </div>
          <p className="m-0 text-[12.5px] font-medium text-[#82827A]">
            You'll be asked to upload this right after you submit this form.
          </p>
        </div>
      </div>

      <TextAreaField
        label="Additional Information"
        optional
        placeholder="Anything else we should know?"
        registration={register('additionalInformation')}
        error={errors.additionalInformation?.message}
      />
    </div>
  );
}
