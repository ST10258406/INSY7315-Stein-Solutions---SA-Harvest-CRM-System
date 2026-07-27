import React from 'react';

interface JwtDevModePanelProps {
  jwtData: {
    token: string;
    header: Record<string, unknown>;
    payload: Record<string, unknown>;
    isRealApi?: boolean;
  } | null;
}

const JwtDevModePanel: React.FC<JwtDevModePanelProps> = ({ jwtData }) => {
  if (!jwtData) {
    return (
      <div className="bg-[#121212]/80 backdrop-blur-xl border border-dashed border-gray-700 rounded-2xl h-full flex flex-col items-center justify-center p-8 text-center text-gray-500">
        <svg xmlns="http://www.w3.org/2000/svg" className="h-12 w-12 mb-4 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
        </svg>
        <p className="text-lg font-medium text-gray-300">JWT Dev Mode Inspector</p>
        <p className="text-sm mt-2">Sign in via API or click "Simulate Mock Login" to inspect real-time decoded JWT headers, claims, and raw tokens.</p>
      </div>
    );
  }

  const isReal = jwtData.isRealApi ?? false;

  // Formatting JSON with syntax highlighting
  const formatJson = (obj: Record<string, unknown>) => {
    return JSON.stringify(obj, null, 2);
  };

  return (
    <div className="bg-[#121212]/90 backdrop-blur-xl border border-gray-800 rounded-2xl shadow-2xl p-6 h-full flex flex-col relative overflow-hidden">
      
      {/* Decorative gradient */}
      <div className="absolute top-0 right-0 w-32 h-32 bg-primary/10 rounded-full blur-3xl pointer-events-none"></div>

      <div className="flex items-center justify-between mb-6 pb-4 border-b border-gray-800">
        <div>
          <h3 className="text-xl font-semibold text-white tracking-tight flex items-center gap-2">
            <span className={`w-2 h-2 rounded-full ${isReal ? 'bg-emerald-400' : 'bg-yellow-400'} animate-pulse`}></span>
            Decoded JWT Inspector
          </h3>
          <span className={`text-xs font-medium inline-block mt-1 ${isReal ? 'text-emerald-400' : 'text-yellow-400'}`}>
            {isReal ? '● Issued by CRM.API (Backend)' : '○ Client Simulated (Mock Dev Mode)'}
          </span>
        </div>
        <span className="text-xs font-mono bg-[#1E1E1E] text-gray-400 px-2.5 py-1 rounded border border-gray-700">
          {String(jwtData.header.alg || 'HS256')}
        </span>
      </div>

      <div className="flex-1 overflow-y-auto space-y-6 pr-2 custom-scrollbar">
        
        {/* Header */}
        <div>
          <div className="text-xs font-semibold text-rose-400 mb-2 uppercase tracking-wider">Header <span className="text-gray-500 lowercase">Algorithm & Token Type</span></div>
          <pre className="bg-[#0A0A0A] p-4 rounded-lg text-sm font-mono text-rose-300 overflow-x-auto border border-gray-800 shadow-inner">
            {formatJson(jwtData.header)}
          </pre>
        </div>

        {/* Payload */}
        <div>
          <div className="text-xs font-semibold text-purple-400 mb-2 uppercase tracking-wider">Payload <span className="text-gray-500 lowercase">Data & Claims</span></div>
          <pre className="bg-[#0A0A0A] p-4 rounded-lg text-sm font-mono text-purple-300 overflow-x-auto border border-gray-800 shadow-inner">
            {formatJson(jwtData.payload)}
          </pre>
        </div>

        {/* Encoded Token */}
        <div>
          <div className="text-xs font-semibold text-cyan-400 mb-2 uppercase tracking-wider">Raw Encoded Token</div>
          <div className="bg-[#0A0A0A] p-4 rounded-lg text-sm font-mono border border-gray-800 shadow-inner break-all relative group">
            <span className="text-rose-400">{jwtData.token.split('.')[0]}</span>
            <span className="text-gray-500">.</span>
            <span className="text-purple-400">{jwtData.token.split('.')[1]}</span>
            <span className="text-gray-500">.</span>
            <span className="text-cyan-400">{jwtData.token.split('.')[2]}</span>
          </div>
        </div>

      </div>
    </div>
  );
};

export default JwtDevModePanel;
