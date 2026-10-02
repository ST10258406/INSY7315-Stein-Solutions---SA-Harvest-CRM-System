import { describe, it, expect } from 'vitest';
import { csvCell } from '@/lib/csv';
import { buildDashboardCsv, type DashboardSnapshot } from './dashboardCsv';

function snapshot(overrides: Partial<DashboardSnapshot> = {}): DashboardSnapshot {
  return {
    generatedAt: new Date(2026, 9, 2, 9, 30),
    filters: [],
    donorCounts: { total: 42, active: 30, pendingReview: 8, lapsed: 4 },
    stats: {
      totalDonors: 42,
      activeDonors: 30,
      pendingApprovals: 3,
      myOpenTasks: 5,
      myOverdueFollowUps: 2,
      donorsContactedThisMonth: 12,
      donorsContactedLastMonth: 9,
    },
    includePendingApprovals: true,
    periodLabel: 'Last 7 days',
    activity: { period: 'weekly', fromUtc: '', toUtc: '', items: [{ userId: '1', name: 'Ada Lovelace', donorsContacted: 10 }] },
    overdue: [],
    overdueTotal: 0,
    ...overrides,
  };
}

describe('csvCell', () => {
  it('quotes values and doubles embedded quotes', () => {
    expect(csvCell('Acme "Fresh", Ltd')).toBe('"Acme ""Fresh"", Ltd"');
  });

  it('neutralises formula-leading values (CSV injection)', () => {
    expect(csvCell('=HYPERLINK("x")')).toBe('"\'=HYPERLINK(""x"")"');
    expect(csvCell('+27 21 555')).toBe('"\'+27 21 555"');
    expect(csvCell('@SUM(A1)')).toBe('"\'@SUM(A1)"');
  });

  it('renders null/undefined as an empty cell and numbers as-is', () => {
    expect(csvCell(null)).toBe('""');
    expect(csvCell(undefined)).toBe('""');
    expect(csvCell(7)).toBe('"7"');
  });
});

describe('buildDashboardCsv', () => {
  it('includes every section and the active filters', () => {
    const csv = buildDashboardCsv(snapshot({ filters: [['Region', 'Western Cape']] }));
    expect(csv).toContain('"Filters","Region: Western Cape"');
    expect(csv).toContain('"Total donors","42"');
    expect(csv).toContain('"Pending approvals","3"');
    expect(csv).toContain('"Donors contacted per team member (Last 7 days)"');
    expect(csv).toContain('"Ada Lovelace","10"');
    expect(csv.split('\r\n').length).toBeGreaterThan(10);
  });

  it('omits pending approvals for non-Admins', () => {
    expect(buildDashboardCsv(snapshot({ includePendingApprovals: false }))).not.toContain('Pending approvals');
  });

  it('notes when the overdue list is truncated', () => {
    const csv = buildDashboardCsv(
      snapshot({
        overdue: [
          {
            id: 'd1',
            companyName: 'Cape Fresh',
            companyType: 'Retailer',
            status: 'Active',
            submissionSource: 'ManualCapture',
            relationshipManager: { id: 'u1', fullName: 'Grace Hopper' },
            followUpDate: '2026-09-01',
            lastInteractionDate: null,
            lastInteractionType: null,
            operationalRegions: [],
            donationFrequency: null,
            donationTypes: [],
          },
        ],
        overdueTotal: 150,
      }),
    );
    expect(csv).toContain('"Overdue follow-ups (first 1 of 150)"');
    expect(csv).toContain('"Cape Fresh","Active","Grace Hopper","2026-09-01"');
  });
});
