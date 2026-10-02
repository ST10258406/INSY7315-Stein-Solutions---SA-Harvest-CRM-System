import { describe, it, expect } from 'vitest';
import { buildDonorListCsv } from './donorCsv';
import type { DonorListItemDto } from '../types';

const donor: DonorListItemDto = {
  id: 'd1',
  companyName: '=Evil Co',
  companyType: 'Retailer',
  status: 'PendingReview',
  submissionSource: 'PublicForm',
  relationshipManager: { id: 'u1', fullName: 'Grace Hopper' },
  followUpDate: '2026-10-15',
  lastInteractionDate: '2026-09-30T08:00:00Z',
  lastInteractionType: 'Call',
  operationalRegions: ['Cape Town', 'Durban'],
  donationFrequency: 'Weekly',
  donationTypes: ['Fresh produce', 'Dry goods'],
};

describe('buildDonorListCsv', () => {
  it('writes a header row plus one row per donor with readable values', () => {
    const [header, row, ...rest] = buildDonorListCsv([donor]).split('\r\n');
    expect(rest).toHaveLength(0);
    expect(header).toContain('"Company name","Company type","Status"');
    expect(row).toBe(
      '"\'=Evil Co","Retailer","Pending review","Grace Hopper","Cape Town; Durban","Weekly","Fresh produce; Dry goods","2026-09-30","Call","2026-10-15","PublicForm"',
    );
  });

  it('writes blanks for missing optional fields', () => {
    const row = buildDonorListCsv([
      { ...donor, companyName: 'Acme', relationshipManager: null, followUpDate: null, lastInteractionDate: null, lastInteractionType: null, donationFrequency: null },
    ]).split('\r\n')[1];
    expect(row).toBe('"Acme","Retailer","Pending review","","Cape Town; Durban","","Fresh produce; Dry goods","","","","PublicForm"');
  });
});
