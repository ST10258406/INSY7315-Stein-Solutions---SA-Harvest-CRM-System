import React from 'react';

const TONNES_RESCUED_THIS_MONTH = 124;

function getTallyMarks(tonnes: number) {
  const scale = Math.max(1, Math.round(tonnes / 22));
  const strokes = Math.round(tonnes / scale);
  const note = scale === 1 ? 'Each mark is a tonne.' : `Each mark is ${scale} tonnes.`;
  return {
    bundles: Array.from({ length: Math.floor(strokes / 5) }),
    remainder: Array.from({ length: strokes % 5 }),
    note,
  };
}

const AuthBrandPanel: React.FC = () => {
  const { bundles, remainder, note } = getTallyMarks(TONNES_RESCUED_THIS_MONTH);

  return (
    <div className="hidden lg:flex w-[44%] min-w-[420px] flex-none flex-col justify-between bg-[#17140F] p-11 relative overflow-hidden">
      <div className="absolute top-0 left-0 w-full h-1 bg-[#F2B705]" />

      <div className="flex items-center gap-[11px]">
        <div className="w-9 h-9 rounded-[10px] bg-[#F2B705] flex items-center justify-center flex-none">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
            <path d="M12 2C12 8 8 9 8 14C8 17.3 9.8 20 12 22C14.2 20 16 17.3 16 14C16 9 12 8 12 2Z" fill="#17140F" />
            <path d="M12 12V22" stroke="#F2B705" strokeWidth={1.4} />
          </svg>
        </div>
        <div className="leading-tight">
          <div className="font-bold text-[15px] text-[#FBF7EE]">SA Harvest</div>
          <div className="text-[11px] text-[#8C8578] font-medium">Donor CRM</div>
        </div>
      </div>

      <div>
        <div className="text-[10px] font-bold tracking-wide text-[#F2B705] uppercase mb-3.5">Rescued this month</div>
        <div className="flex items-end gap-[9px] min-h-[38px] mb-3.5">
          {bundles.map((_, i) => (
            <div key={`bundle-${i}`} className="relative w-[22px] h-[38px] flex-none">
              <div className="absolute left-px top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
              <div className="absolute left-[7px] top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
              <div className="absolute left-[13px] top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
              <div className="absolute left-[19px] top-0 w-[2.5px] h-[38px] bg-[#F4F2EC] rounded-[1px]" />
              <div className="absolute -left-0.5 top-4 w-[26px] h-[2.5px] bg-[#F2B705] rounded-[1px] -rotate-[30deg]" />
            </div>
          ))}
          {remainder.map((_, i) => (
            <div key={`remainder-${i}`} className="w-[2.5px] h-[38px] bg-[#F4F2EC] flex-none rounded-[1px]" />
          ))}
        </div>
        <div className="text-[31px] font-bold text-[#FBF7EE] leading-tight max-w-[400px]">
          {TONNES_RESCUED_THIS_MONTH} tonnes of surplus food moved to communities that needed it.
        </div>
        <div className="text-[13px] text-[#8C8578] mt-3 max-w-[380px]">
          {note} Every donor relationship you keep warm in here is part of that number.
        </div>
      </div>

      <div className="flex items-center gap-[18px] text-[11.5px] text-[#6E6857]">
        <span>© 2026 SA Harvest NPC</span>
        <a href="#" className="text-[#8C8578] hover:text-[#B65C36]">Privacy</a>
        <a href="#" className="text-[#8C8578] hover:text-[#B65C36]">Support</a>
      </div>
    </div>
  );
};

export default AuthBrandPanel;
