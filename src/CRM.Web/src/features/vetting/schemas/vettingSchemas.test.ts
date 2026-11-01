import { describe, it, expect } from 'vitest';
import { officerFormSchema, type OfficerFormValues } from './officerForm.schema';
import { siteVisitFormSchema, siteVisitFormValuesToRequest, type SiteVisitFormValues } from './siteVisitForm.schema';

const officer: OfficerFormValues = { fullName: 'Thandi Mokoena', initials: 'TM', assignedRegion: 'Gauteng North' };
const visit: SiteVisitFormValues = {
  donorId: 'd1',
  officerId: 'o1',
  scheduledDate: '2026-10-21',
  scheduledTime: '09:30',
  locationLabel: '',
  visitNotes: '',
};

const failsOn = (result: { success: boolean; error?: { issues: { path: PropertyKey[] }[] } }, field: string) => {
  expect(result.success).toBe(false);
  expect([...new Set(result.error!.issues.map((i) => i.path[0]))]).toEqual([field]);
};

describe('officerFormSchema', () => {
  it('accepts a valid officer', () => expect(officerFormSchema.safeParse(officer).success).toBe(true));

  it.each<[keyof OfficerFormValues, string]>([
    ['fullName', ''],
    ['fullName', 'a'.repeat(101)],
    ['initials', ''],
    ['initials', 'ABCDEF'],
    ['initials', 'tm'],
    ['initials', 'T.M'],
    ['assignedRegion', ''],
    ['assignedRegion', 'a'.repeat(51)],
  ])('rejects %s = %j', (field, value) => failsOn(officerFormSchema.safeParse({ ...officer, [field]: value }), field));
});

describe('siteVisitFormSchema', () => {
  it('accepts a valid visit with blank optional fields', () => expect(siteVisitFormSchema.safeParse(visit).success).toBe(true));

  it.each<[keyof SiteVisitFormValues, string]>([
    ['donorId', ''],
    ['officerId', ''],
    ['scheduledDate', ''],
    ['scheduledTime', ''],
    ['locationLabel', 'a'.repeat(101)],
    ['visitNotes', 'a'.repeat(256)],
  ])('rejects %s = %j', (field, value) => failsOn(siteVisitFormSchema.safeParse({ ...visit, [field]: value }), field));

  it('maps to the API body: time gets seconds, blank optionals are left out', () =>
    expect(siteVisitFormValuesToRequest({ ...visit, visitNotes: ' Gate 3 ' })).toEqual({
      donorId: 'd1',
      officerId: 'o1',
      scheduledDate: '2026-10-21',
      scheduledTime: '09:30:00',
      visitNotes: 'Gate 3',
    }));
});
