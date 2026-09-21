import { SignaturePad } from '../SignaturePad';

interface SignatureStepProps {
  onChange: (dataUrl: string | null) => void;
  error?: string;
}

export function SignatureStep({ onChange, error }: SignatureStepProps) {
  return <SignaturePad onChange={onChange} error={error} />;
}
