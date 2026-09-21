import { formValuesToCreateRequest } from '@/features/donors/schemas/donorFormMapping';
import type { DonorFormValues } from '@/features/donors/schemas/donorFormSchema';
import type { CreateDonorRequest } from '@/features/donors/types';

export interface SubmitPublicDonorRequest extends CreateDonorRequest {
  signature: { imageBase64: string };
}

// SubmitPublicDonorRequest is CreateDonorRequest plus a signature on the
// backend (SubmitPublicDonorRequest : CreateDonorRequest) — same field-by-field
// mapping as the internal /donors create flow, so this reuses it rather than
// re-deriving the same company/contact/address/donation logic a second time.
export function formValuesToPublicSubmitRequest(values: DonorFormValues, signatureImageBase64: string): SubmitPublicDonorRequest {
  return {
    ...formValuesToCreateRequest(values),
    signature: { imageBase64: signatureImageBase64 },
  };
}
