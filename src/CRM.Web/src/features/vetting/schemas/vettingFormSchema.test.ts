import { describe, it, expect } from 'vitest';
import { taskMinDueDate as todayYmd } from '@/features/tasks/schemas/taskForm.schema';
import { vettingFormSchema, type VettingFormValues } from './vettingFormSchema';
import { validFormValues } from './vettingFormFixtures';
import { formValuesToUpdateRequest, submissionToFormValues } from './vettingFormMapping';
import type { VettingAssessmentDetails, VettingSubmission } from '../types';

type Details = VettingFormValues['details'];

function failsOnlyOn(field: keyof Details, patch: Partial<Details>) {
  const values = validFormValues();
  const result = vettingFormSchema.safeParse({ ...values, details: { ...values.details, ...patch } });
  expect(result.success).toBe(false);
  expect([...new Set(result.error!.issues.map((i) => i.path.join('.')))]).toEqual([`details.${field}`]);
}

describe('vettingFormSchema', () => {
  it('accepts a complete form', () => expect(vettingFormSchema.safeParse(validFormValues()).success).toBe(true));

  it('allows an empty infrastructureChecks (none passed)', () => {
    const values = validFormValues();
    values.details.infrastructureChecks = [];
    expect(vettingFormSchema.safeParse(values).success).toBe(true);
  });

  it.each<[string, keyof Details, Partial<Details>]>([
    ['1 bad email', 'contactEmail', { contactEmail: 'not-an-email' }],
    ['1 non-http website', 'website', { website: 'ftp://example.com' }],
    ['2 bad what3words', 'what3Words', { what3Words: 'apple.orange.banana' }],
    ['2 unknown province', 'province', { province: 'Gauteng North' }],
    ['3 empty required array', 'targetPopulation', { targetPopulation: [] }],
    ['3 value outside its list', 'coreFocusAreas', { coreFocusAreas: ['Feeding', 'Space travel'] }],
    ['4 negative count', 'fullTimeMales', { fullTimeMales: -1 }],
    ['4 blank count', 'volunteersCount', { volunteersCount: Number.NaN }],
    ['5 unanswered boolean', 'isRegisteredNpo', { isRegisteredNpo: '' }],
    ['6 hyphen instead of en dash', 'beneficiaryAgeGroups', { beneficiaryAgeGroups: ['0-5'] }],
    ['7 future lastFedDate', 'lastFedDate', { lastFedDate: '2999-01-01' }],
    ['7 unknown transport', 'transportCapacity', { transportCapacity: 'Bicycle' }],
    ['8 unanswered hygiene', 'pestFree', { pestFree: '' }],
    ['8 unknown infrastructure', 'infrastructureChecks', { infrastructureChecks: ['Plumbing'] }],
    ['9 police proximity too long', 'policeStationProximity', { policeStationProximity: 'a'.repeat(101) }],
    ['10 comments too long', 'officerComments', { officerComments: 'a'.repeat(4001) }],
    ['11 unanswered document', 'slaSigned', { slaSigned: '' }],
  ])('section %s', (_label, field, patch) => failsOnlyOn(field, patch));

  it('accepts today as lastFedDate', () => {
    const values = validFormValues();
    values.details.lastFedDate = todayYmd();
    expect(vettingFormSchema.safeParse(values).success).toBe(true);
  });
});

describe('vettingFormMapping', () => {
  it('round-trips API details → form values → update request unchanged', () => {
    const details: VettingAssessmentDetails = {
      ...(validFormValues().details as unknown as VettingAssessmentDetails),
      website: null, addressPhysical: '12 Industrial Rd', addressDetail: null,
      isRegisteredNpo: false, isRegisteredDsd: true, kitchenClean: true, waterAccess: true, toiletFacilities: false,
      pestFree: true, deliveryAccessEase: true, parkingAndSecurity: false, slaSigned: true, consentFormSigned: false,
      policyDocumentPresent: true, lastFedDate: null, policeStationProximity: null,
      officerComments: 'Well run', proposalCapabilities: null, digitalCapabilities: null,
    };
    const submission = { donorId: 'd1', officerId: 'o1', siteVisitId: null, details } as VettingSubmission;

    expect(formValuesToUpdateRequest(submissionToFormValues(submission))).toEqual({ officerId: 'o1', details });
  });

  it('trims text and sends blank optionals as null', () => {
    const values = validFormValues();
    values.details.legalName = '  Ubuntu  ';
    values.details.website = '   ';
    const { details } = formValuesToUpdateRequest(values);
    expect(details.legalName).toBe('Ubuntu');
    expect(details.website).toBeNull();
    expect(details.isRegisteredNpo).toBe(false);
  });
});
