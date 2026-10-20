import { describe, it, expect } from 'vitest';
import { vettingDecisionSchema } from './vettingDecision.schema';

describe('vettingDecisionSchema', () => {
  it.each([
    ['Approve', '', true],
    ['Approve', 'Looks good', true],
    ['Return', 'Toilets need re-inspection', true],
    ['Return', '', false],
    ['Return', '    ', false],
    ['Decline', '', false],
    ['Decline', 'Not a food-relief organisation', true],
  ] as const)('%s with reason %j → valid %s', (decision, reason, valid) =>
    expect(vettingDecisionSchema.safeParse({ decision, reason }).success).toBe(valid));

  it('caps the reason at 2000 characters for every decision', () =>
    expect(vettingDecisionSchema.safeParse({ decision: 'Approve', reason: 'a'.repeat(2001) }).success).toBe(false));

  it('trims the reason', () =>
    expect(vettingDecisionSchema.parse({ decision: 'Return', reason: '  Fix it  ' }).reason).toBe('Fix it'));
});
