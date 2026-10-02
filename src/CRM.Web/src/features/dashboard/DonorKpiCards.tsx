import { Users, Clock, Database, UserX } from 'lucide-react';
import { useDonors } from '@/features/donors/hooks';
import type { DashboardScopeFilters, DashboardSegmentFilters } from './types';

interface DonorKpiCardsProps {
  /** Dashboard scope + Donor activity segment filters, applied to every count. */
  filters?: DashboardScopeFilters & DashboardSegmentFilters;
}

export function DonorKpiCards({ filters = {} }: DonorKpiCardsProps) {
  const total = useDonors({ ...filters, pageSize: 1 });
  const active = useDonors({ ...filters, status: 'Active', pageSize: 1 });
  const pending = useDonors({ ...filters, status: 'PendingReview', pageSize: 1 });
  const lapsed = useDonors({ ...filters, status: 'Lapsed', pageSize: 1 });

  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
      {/* Card 1: Total Donors */}
      <div className="bg-[var(--card)] rounded-xl p-[18px_18px_0] shadow-[0_1px_3px_var(--shadow)]">
        <div className="flex items-center gap-2.25 mb-4">
          <div className="w-7.5 h-7.5 rounded-lg bg-[var(--icon-bg)] flex items-center justify-center text-[var(--ink)]">
            <Database className="w-4 h-4" />
          </div>
          <span className="text-sm font-bold text-[var(--ink)]">Total Donors</span>
        </div>
        <div className="flex items-center gap-1.25 mb-0.5">
          <span className="text-xs font-bold text-emerald-600 dark:text-emerald-500">+11.5%</span>
        </div>
        <div className="text-[28px] font-extrabold tracking-tight mb-1.5 text-[var(--ink)]">
          {total.isPending ? (
            <div className="h-8.5 w-16 animate-pulse rounded-md bg-[var(--skel)]" aria-hidden />
          ) : total.isError ? (
            <span className="text-[var(--muted-c)]" title="Couldn't load this count">—</span>
          ) : (
            total.data?.pagination.totalCount ?? 0
          )}
        </div>
        <svg viewBox="0 0 220 62" preserveAspectRatio="none" className="block w-full h-[62px]">
          <defs>
            <linearGradient id="sparkA" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#FADF01" stopOpacity="0.55" />
              <stop offset="1" stopColor="#FADF01" stopOpacity="0" />
            </linearGradient>
          </defs>
          <path
            d="M0 44 L18 38 L36 46 L54 30 L72 36 L90 22 L108 30 L126 16 L144 26 L162 12 L180 20 L198 8 L220 14 L220 62 L0 62 Z"
            fill="url(#sparkA)"
          />
          <path
            d="M0 44 L18 38 L36 46 L54 30 L72 36 L90 22 L108 30 L126 16 L144 26 L162 12 L180 20 L198 8 L220 14"
            fill="none"
            stroke="#FADF01"
            strokeWidth="2.2"
            strokeLinejoin="round"
          />
        </svg>
      </div>

      {/* Card 2: Active Donors */}
      <div className="bg-[var(--card)] rounded-xl p-[18px_18px_0] shadow-[0_1px_3px_var(--shadow)]">
        <div className="flex items-center gap-2.25 mb-4">
          <div className="w-7.5 h-7.5 rounded-lg bg-[var(--icon-bg)] flex items-center justify-center text-[var(--ink)]">
            <Users className="w-4 h-4" />
          </div>
          <span className="text-sm font-bold text-[var(--ink)]">Active Donors</span>
        </div>
        <div className="flex items-center gap-1.25 mb-0.5">
          <span className="text-xs font-bold text-emerald-600 dark:text-emerald-500">+4.5%</span>
        </div>
        <div className="text-[28px] font-extrabold tracking-tight mb-1.5 text-[var(--ink)]">
          {active.isPending ? (
            <div className="h-8.5 w-16 animate-pulse rounded-md bg-[var(--skel)]" aria-hidden />
          ) : active.isError ? (
            <span className="text-[var(--muted-c)]" title="Couldn't load this count">—</span>
          ) : (
            active.data?.pagination.totalCount ?? 0
          )}
        </div>
        <svg viewBox="0 0 220 62" preserveAspectRatio="none" className="block w-full h-[62px]">
          <defs>
            <linearGradient id="sparkB" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#FADF01" stopOpacity="0.55" />
              <stop offset="1" stopColor="#FADF01" stopOpacity="0" />
            </linearGradient>
          </defs>
          <path
            d="M0 34 L18 40 L36 28 L54 34 L72 20 L90 32 L108 24 L126 34 L144 22 L162 28 L180 16 L198 24 L220 18 L220 62 L0 62 Z"
            fill="url(#sparkB)"
          />
          <path
            d="M0 34 L18 40 L36 28 L54 34 L72 20 L90 32 L108 24 L126 34 L144 22 L162 28 L180 16 L198 24 L220 18"
            fill="none"
            stroke="#FADF01"
            strokeWidth="2.2"
            strokeLinejoin="round"
          />
        </svg>
      </div>

      {/* Card 3: Pending Approvals (Urgent Dark Card) */}
      <div className="bg-[var(--urgent)] rounded-xl p-[18px_18px_0] shadow-[0_4px_14px_rgba(20,20,15,0.22)]">
        <div className="flex items-center gap-2.25 mb-4">
          <div className="w-7.5 h-7.5 rounded-lg bg-brand/15 flex items-center justify-center text-brand">
            <Clock className="w-4 h-4" />
          </div>
          <span className="text-sm font-bold text-white">Pending Approvals</span>
        </div>
        <div className="flex items-center gap-1.25 mb-0.5">
          <span className="text-xs font-bold text-brand">+8 this week</span>
        </div>
        <div className="text-[28px] font-extrabold tracking-tight mb-1.5 text-white">
          {pending.isPending ? (
            <div className="h-8.5 w-16 animate-pulse rounded-md bg-brand/20" aria-hidden />
          ) : pending.isError ? (
            <span className="text-white/50" title="Couldn't load this count">—</span>
          ) : (
            pending.data?.pagination.totalCount ?? 0
          )}
        </div>
        <svg viewBox="0 0 220 62" preserveAspectRatio="none" className="block w-full h-[62px]">
          <defs>
            <linearGradient id="sparkC" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#FADF01" stopOpacity="0.45" />
              <stop offset="1" stopColor="#FADF01" stopOpacity="0" />
            </linearGradient>
          </defs>
          <path
            d="M0 40 L18 30 L36 38 L54 24 L72 32 L90 18 L108 28 L126 20 L144 30 L162 18 L180 26 L198 14 L220 22 L220 62 L0 62 Z"
            fill="url(#sparkC)"
          />
          <path
            d="M0 40 L18 30 L36 38 L54 24 L72 32 L90 18 L108 28 L126 20 L144 30 L162 18 L180 26 L198 14 L220 22"
            fill="none"
            stroke="#FADF01"
            strokeWidth="2.2"
            strokeLinejoin="round"
          />
        </svg>
      </div>

      {/* Card 4: Lapsed Donors */}
      <div className="bg-[var(--card)] rounded-xl p-[18px_18px_0] shadow-[0_1px_3px_var(--shadow)]">
        <div className="flex items-center gap-2.25 mb-4">
          <div className="w-7.5 h-7.5 rounded-lg bg-[var(--icon-bg)] flex items-center justify-center text-[var(--ink)]">
            <UserX className="w-4 h-4" />
          </div>
          <span className="text-sm font-bold text-[var(--ink)]">Lapsed Donors</span>
        </div>
        <div className="flex items-center gap-1.25 mb-0.5">
          <span className="text-xs font-bold text-[var(--muted-c)]">Needs follow-up</span>
        </div>
        <div className="text-[28px] font-extrabold tracking-tight mb-1.5 text-[var(--ink)]">
          {lapsed.isPending ? (
            <div className="h-8.5 w-16 animate-pulse rounded-md bg-[var(--skel)]" aria-hidden />
          ) : lapsed.isError ? (
            <span className="text-[var(--muted-c)]" title="Couldn't load this count">—</span>
          ) : (
            lapsed.data?.pagination.totalCount ?? 0
          )}
        </div>
        <svg viewBox="0 0 220 62" preserveAspectRatio="none" className="block w-full h-[62px]">
          <defs>
            <linearGradient id="sparkD" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#FADF01" stopOpacity="0.55" />
              <stop offset="1" stopColor="#FADF01" stopOpacity="0" />
            </linearGradient>
          </defs>
          <path
            d="M0 20 L18 26 L36 18 L54 30 L72 22 L90 34 L108 26 L126 38 L144 30 L162 40 L180 34 L198 44 L220 38 L220 62 L0 62 Z"
            fill="url(#sparkD)"
          />
          <path
            d="M0 20 L18 26 L36 18 L54 30 L72 22 L90 34 L108 26 L126 38 L144 30 L162 40 L180 34 L198 44 L220 38"
            fill="none"
            stroke="#FADF01"
            strokeWidth="2.2"
            strokeLinejoin="round"
          />
        </svg>
      </div>
    </div>
  );
}
