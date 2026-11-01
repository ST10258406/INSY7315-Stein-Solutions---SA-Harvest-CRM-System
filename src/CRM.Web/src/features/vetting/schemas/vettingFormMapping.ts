import type {
  CreateSubmissionRequest,
  UpdateSubmissionRequest,
  VettingAssessmentDetails,
  VettingSubmission,
} from '../types';
import type { VettingFormValues } from './vettingFormSchema';

type FormDetails = VettingFormValues['details'];

const BOOLEAN_FIELDS = [
  'isRegisteredNpo', 'isRegisteredDsd', 'kitchenClean', 'waterAccess', 'toiletFacilities', 'pestFree',
  'deliveryAccessEase', 'parkingAndSecurity', 'slaSigned', 'consentFormSigned', 'policyDocumentPresent',
] as const;

const OPTIONAL_TEXT_FIELDS = [
  'website', 'addressPhysical', 'addressDetail', 'policeStationProximity',
  'officerComments', 'proposalCapabilities', 'digitalCapabilities',
] as const;

/** Counts start at 0, lists empty, booleans unanswered (''), text blank. */
export function emptyVettingFormValues(prefill: Partial<Pick<VettingFormValues, 'donorId' | 'officerId' | 'siteVisitId'>> = {}): VettingFormValues {
  return {
    donorId: prefill.donorId ?? '',
    officerId: prefill.officerId ?? '',
    siteVisitId: prefill.siteVisitId ?? '',
    details: {
      legalName: '', contactName: '', contactEmail: '', contactPhone: '', website: '',
      addressPhysical: '', addressDetail: '', province: '', what3Words: '',
      coreFocusAreas: [], targetPopulation: [], programmesSummary: '', distributionChannel: '',
      fullTimeFemales: 0, fullTimeMales: 0, volunteersCount: 0,
      isRegisteredNpo: '', isRegisteredDsd: '',
      beneficiaryRaces: [], beneficiaryGenders: [], beneficiaryAgeGroups: [],
      feedingFrequency: '',
      totalPeopleServed: 0, femalesServed: 0, malesServed: 0, africanServed: 0, colouredServed: 0, indianAsianServed: 0, whiteServed: 0,
      saHarvestReliance: '', transportCapacity: '',
      mealsProvided: [], feedingDays: [], lastFedDate: '',
      foodStorageTypes: [],
      kitchenClean: '', waterAccess: '',
      toiletFacilities: '', pestFree: '',
      infrastructureChecks: [],
      deliveryAccessEase: '', parkingAndSecurity: '',
      policeStationProximity: '',
      officerComments: '', proposalCapabilities: '', digitalCapabilities: '',
      slaSigned: '', consentFormSigned: '',
      policyDocumentPresent: '',
    },
  };
}

function detailsToForm(d: VettingAssessmentDetails): FormDetails {
  const form = { ...d, lastFedDate: d.lastFedDate ?? '' } as unknown as Record<string, unknown>;
  for (const key of OPTIONAL_TEXT_FIELDS) form[key] = d[key] ?? '';
  for (const key of BOOLEAN_FIELDS) form[key] = d[key] === null ? '' : String(d[key]);
  for (const [key, value] of Object.entries(d)) if (Array.isArray(value)) form[key] = [...value];
  return form as FormDetails;
}

/** Trims text, sends blank optional text and an empty date as null, and turns 'true'/'false' back into booleans. */
function formToDetails(f: FormDetails): VettingAssessmentDetails {
  const out = { ...f } as Record<string, unknown>;
  for (const [key, value] of Object.entries(f)) if (typeof value === 'string') out[key] = value.trim();
  for (const key of OPTIONAL_TEXT_FIELDS) out[key] = (out[key] as string) || null;
  for (const key of BOOLEAN_FIELDS) out[key] = f[key] === '' ? null : f[key] === 'true';
  out.lastFedDate = f.lastFedDate || null;
  return out as unknown as VettingAssessmentDetails;
}

export function submissionToFormValues(s: VettingSubmission): VettingFormValues {
  return { donorId: s.donorId, officerId: s.officerId, siteVisitId: s.siteVisitId ?? '', details: detailsToForm(s.details) };
}

export function formValuesToCreateRequest(v: VettingFormValues): CreateSubmissionRequest {
  return {
    donorId: v.donorId,
    officerId: v.officerId,
    ...(v.siteVisitId ? { siteVisitId: v.siteVisitId } : {}),
    details: formToDetails(v.details),
  };
}

export function formValuesToUpdateRequest(v: VettingFormValues): UpdateSubmissionRequest {
  return { officerId: v.officerId, details: formToDetails(v.details) };
}
