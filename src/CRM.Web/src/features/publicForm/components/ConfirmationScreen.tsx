import { useState } from 'react';
import { Copy, Check, PartyPopper } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { paths } from '@/routes/paths';
import { PublicPageShell } from './PublicPageShell';
import { PublicCard } from './PublicCard';

interface ConfirmationScreenProps {
  message: string;
  referenceNumber: string;
}

/**
 * The last screen of the public donor flow — reachable only from
 * PublicFormPage's in-memory `phase === 'complete'` state after a genuinely
 * successful submission (see PublicFormPage.tsx). There's no route for this
 * on its own and the reference number never lives in the URL, so a refresh
 * or a direct visit can't produce a fabricated/blank confirmation for a
 * submission that didn't happen — it just lands back at the start of the
 * form, which is the existing behaviour of this being in-memory state only.
 */
export function ConfirmationScreen({ message, referenceNumber }: ConfirmationScreenProps) {
  const navigate = useNavigate();
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(referenceNumber);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Clipboard access can be denied/unavailable (permissions, non-secure
      // context, older browsers) — the reference number is already visible
      // on screen for the donor to note down manually, so this is a silent
      // no-op rather than an error state.
    }
  }

  return (
    <PublicPageShell>
      <PublicCard>
        <div className="flex flex-col items-center gap-4 py-4 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-[#FBF6D4]">
            <PartyPopper className="h-6 w-6 text-[#8A7A00]" strokeWidth={1.8} />
          </div>

          <div>
            <h2 className="mb-1.5 text-[20px] font-extrabold tracking-tight text-[#16160F]">Thank you!</h2>
            <p className="mx-auto max-w-[420px] text-[13.5px] leading-relaxed font-medium text-[#82827A]">{message}</p>
          </div>

          <div className="flex flex-col items-center gap-2 rounded-[14px] border border-[#E4E4DE] bg-white px-6 py-4">
            <span className="text-[11px] font-bold tracking-[1.5px] text-[#9A9A90] uppercase">Reference Number</span>
            <div className="flex items-center gap-2.5">
              <span className="text-[19px] font-extrabold tracking-tight text-[#16160F]">{referenceNumber}</span>
              <button
                type="button"
                onClick={handleCopy}
                aria-label="Copy reference number"
                className="flex h-8 w-8 cursor-pointer items-center justify-center rounded-full border-[1.5px] border-[#E4E4DE] bg-white text-[#16160F]"
              >
                {copied ? <Check className="h-3.5 w-3.5 stroke-[3]" /> : <Copy className="h-3.5 w-3.5" />}
              </button>
            </div>
            {copied && <span className="text-[11px] font-semibold text-[#16160F]">Copied to clipboard</span>}
          </div>

          <p className="mx-auto max-w-[420px] text-[12.5px] leading-relaxed font-medium text-[#82827A]">
            Our team will review your submission and reach out using the contact details you provided. Keep your
            reference number handy if you need to follow up.
          </p>

          <Button type="button" onClick={() => navigate(paths.root)} className="mt-1">
            Return to homepage
          </Button>
        </div>
      </PublicCard>
    </PublicPageShell>
  );
}
