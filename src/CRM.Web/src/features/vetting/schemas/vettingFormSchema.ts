import { z } from 'zod';
import { taskMinDueDate as todayYmd } from '@/features/tasks/schemas/taskForm.schema';
import {
  AGE_GROUPS, CORE_FOCUS_AREAS, DAYS, DISTRIBUTION_CHANNELS, FEEDING_FREQUENCIES, GENDERS, INFRASTRUCTURE_CHECKS,
  MEALS, PROVINCES, RACES, RELIANCE_LEVELS, STORAGE_TYPES, TARGET_POPULATIONS, TRANSPORT_CAPACITIES,
} from '../lib/vettingOptions';

// Mirrors VettingAssessmentRules (CRM.Application, #157) section by section: same max lengths,
// same allowed values, ≥1 item for required lists (infrastructureChecks may be empty), same
// what3words regex, no future lastFedDate. Free text without a column cap is held to 4000 like the API.
const LONG_TEXT = 4000;

const text = (max: number) => z.string().trim().max(max, `Must be ${max} characters or fewer`);
const required = (max: number, message: string) => text(max).min(1, message);
// Strings, not z.enum, so the form can hold '' (nothing picked yet); the check is the same.
const oneOf = (values: readonly string[], message: string) => z.string().refine((v) => values.includes(v), message);
const anyOf = (values: readonly string[]) =>
  z.array(z.string()).refine((list) => list.every((v) => values.includes(v)), 'Contains a value that is not an option');
const someOf = (values: readonly string[], message: string) => anyOf(values).refine((list) => list.length > 0, message);
const count = z.number({ message: 'Enter a number' }).int('Whole numbers only').min(0, 'Cannot be negative');

/** A Yes/No select: '' until answered. An audit must record an explicit answer, never a silent false. */
const yesNo = z.string().refine((v) => v === 'true' || v === 'false', 'Please answer Yes or No');

const isHttpUrl = (v: string) => {
  try {
    return ['http:', 'https:'].includes(new URL(v).protocol);
  } catch {
    return false;
  }
};

export const vettingDetailsSchema = z.object({
  // 1 Organisation & Contact
  legalName: required(200, 'Enter the organisation’s legal name'),
  contactName: required(150, 'Enter the contact’s name'),
  contactEmail: required(100, 'Enter the contact’s email').pipe(z.email('Enter a valid email address')),
  contactPhone: required(30, 'Enter the contact’s phone number'),
  website: text(150).refine((v) => v === '' || isHttpUrl(v), 'Enter a full http(s) address'),
  // 2 Location
  addressPhysical: text(LONG_TEXT),
  addressDetail: text(LONG_TEXT),
  province: oneOf(PROVINCES, 'Select a province'),
  what3Words: required(100, 'Enter the what3words address').regex(/^\/\/\/[a-z]+\.[a-z]+\.[a-z]+$/, 'Use the format ///word.word.word'),
  // 3 Programmes & Services
  coreFocusAreas: someOf(CORE_FOCUS_AREAS, 'Pick at least one focus area'),
  targetPopulation: someOf(TARGET_POPULATIONS, 'Pick at least one target group'),
  programmesSummary: required(LONG_TEXT, 'Summarise the programmes and services'),
  distributionChannel: oneOf(DISTRIBUTION_CHANNELS, 'Select a distribution channel'),
  // 4 Staffing
  fullTimeFemales: count,
  fullTimeMales: count,
  volunteersCount: count,
  // 5 Registration
  isRegisteredNpo: yesNo,
  isRegisteredDsd: yesNo,
  // 6 Demographics
  beneficiaryRaces: someOf(RACES, 'Pick at least one race'),
  beneficiaryGenders: someOf(GENDERS, 'Pick at least one gender'),
  beneficiaryAgeGroups: someOf(AGE_GROUPS, 'Pick at least one age group'),
  // 7 Feeding Operation
  feedingFrequency: oneOf(FEEDING_FREQUENCIES, 'Select a feeding frequency'),
  totalPeopleServed: count,
  femalesServed: count,
  malesServed: count,
  africanServed: count,
  colouredServed: count,
  indianAsianServed: count,
  whiteServed: count,
  saHarvestReliance: oneOf(RELIANCE_LEVELS, 'Select the level of reliance'),
  transportCapacity: oneOf(TRANSPORT_CAPACITIES, 'Select the transport capacity'),
  mealsProvided: someOf(MEALS, 'Pick at least one meal'),
  feedingDays: someOf(DAYS, 'Pick at least one day'),
  lastFedDate: z.string().refine((v) => v === '' || v <= todayYmd(), 'Cannot be in the future'),
  // 8 Facilities & Hygiene — infrastructureChecks may be empty (none passed)
  foodStorageTypes: someOf(STORAGE_TYPES, 'Pick at least one storage type'),
  kitchenClean: yesNo,
  waterAccess: yesNo,
  toiletFacilities: yesNo,
  pestFree: yesNo,
  infrastructureChecks: anyOf(INFRASTRUCTURE_CHECKS),
  // 9 Access & Security
  deliveryAccessEase: yesNo,
  parkingAndSecurity: yesNo,
  policeStationProximity: text(100),
  // 10 Capacity Notes
  officerComments: text(LONG_TEXT),
  proposalCapabilities: text(LONG_TEXT),
  digitalCapabilities: text(LONG_TEXT),
  // 11 Documents
  slaSigned: yesNo,
  consentFormSigned: yesNo,
  policyDocumentPresent: yesNo,
});

export const vettingFormSchema = z.object({
  donorId: z.string().min(1, 'Select a donor'),
  officerId: z.string().min(1, 'Select an officer'),
  siteVisitId: z.string(),
  details: vettingDetailsSchema,
});

export type VettingFormValues = z.input<typeof vettingFormSchema>;
export type VettingDetailsKey = keyof VettingFormValues['details'];
