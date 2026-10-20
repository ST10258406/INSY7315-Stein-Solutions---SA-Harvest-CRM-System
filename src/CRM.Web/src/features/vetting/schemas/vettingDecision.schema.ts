import { z } from 'zod';
import type { VettingDecision } from '../types';

// Mirrors DecideSubmissionCommandValidator (#159): a reason is required (non-empty after
// trimming) to Return or Decline and optional to Approve; at most 2000 characters either way.
export const DECISION_REASON_MAX = 2_000;

export const vettingDecisionSchema = z
  .object({
    decision: z.enum(['Approve', 'Return', 'Decline']),
    reason: z.string().trim().max(DECISION_REASON_MAX, `Keep it under ${DECISION_REASON_MAX.toLocaleString()} characters`),
  })
  .refine((v) => v.decision === 'Approve' || v.reason.length > 0, {
    path: ['reason'],
    message: 'Give a reason so the officer knows what happened',
  });

export type VettingDecisionFormValues = z.input<typeof vettingDecisionSchema>;
export type { VettingDecision };
