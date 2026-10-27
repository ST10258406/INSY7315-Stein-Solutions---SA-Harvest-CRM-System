import {
  BadgeCheck,
  Building2,
  Camera,
  ChartPie,
  FileCheck,
  HandHeart,
  MapPin,
  NotebookPen,
  ShieldCheck,
  Soup,
  Truck,
  Users,
  type LucideIcon,
} from 'lucide-react';
import type { VettingDetailsKey } from '../schemas/vettingFormSchema';

/** The 12 accordion sections of the vetting form and which details fields live in each (for error counts). */
export interface SectionMeta {
  id: string;
  title: string;
  icon: LucideIcon;
  fields: VettingDetailsKey[];
}

export const VETTING_SECTIONS: SectionMeta[] = [
  { id: 'organisation', title: 'Organisation & Contact', icon: Building2, fields: ['legalName', 'contactName', 'contactEmail', 'contactPhone', 'website'] },
  { id: 'location', title: 'Location', icon: MapPin, fields: ['addressPhysical', 'addressDetail', 'province', 'what3Words'] },
  { id: 'programmes', title: 'Programmes & Services', icon: HandHeart, fields: ['coreFocusAreas', 'targetPopulation', 'programmesSummary', 'distributionChannel'] },
  { id: 'staffing', title: 'Staffing', icon: Users, fields: ['fullTimeFemales', 'fullTimeMales', 'volunteersCount'] },
  { id: 'registration', title: 'Registration', icon: BadgeCheck, fields: ['isRegisteredNpo', 'isRegisteredDsd'] },
  { id: 'demographics', title: 'Demographics', icon: ChartPie, fields: ['beneficiaryRaces', 'beneficiaryGenders', 'beneficiaryAgeGroups'] },
  {
    id: 'feeding',
    title: 'Feeding Operation',
    icon: Soup,
    fields: [
      'feedingFrequency', 'saHarvestReliance', 'transportCapacity', 'totalPeopleServed', 'femalesServed', 'malesServed',
      'africanServed', 'colouredServed', 'indianAsianServed', 'whiteServed', 'mealsProvided', 'feedingDays', 'lastFedDate',
    ],
  },
  {
    id: 'facilities',
    title: 'Facilities & Hygiene',
    icon: ShieldCheck,
    fields: ['foodStorageTypes', 'kitchenClean', 'waterAccess', 'toiletFacilities', 'pestFree', 'infrastructureChecks'],
  },
  { id: 'access', title: 'Access & Security', icon: Truck, fields: ['deliveryAccessEase', 'parkingAndSecurity', 'policeStationProximity'] },
  { id: 'capacity', title: 'Capacity Notes', icon: NotebookPen, fields: ['officerComments', 'proposalCapabilities', 'digitalCapabilities'] },
  { id: 'documents', title: 'Documents', icon: FileCheck, fields: ['slaSigned', 'consentFormSigned', 'policyDocumentPresent'] },
  { id: 'photos', title: 'Evidence Photos', icon: Camera, fields: [] },
];

export const SECTION_IDS = VETTING_SECTIONS.map((s) => s.id);
/** Every details field in form order — "first invalid field" means first in this list. */
export const FIELD_ORDER = VETTING_SECTIONS.flatMap((s) => s.fields);
export const FIELD_SECTION = Object.fromEntries(VETTING_SECTIONS.flatMap((s) => s.fields.map((f) => [f, s.id]))) as Record<VettingDetailsKey, string>;

/** One label per details field, shared by the form inputs and the read-only detail page. */
export const FIELD_LABELS: Record<VettingDetailsKey, string> = {
  legalName: 'Organisation legal name',
  contactName: 'Contact name and surname',
  contactEmail: 'Contact email',
  contactPhone: 'Contact phone',
  website: 'Website',
  addressPhysical: 'Organisation address',
  addressDetail: 'Address detail',
  province: 'Province',
  what3Words: 'What3Words',
  coreFocusAreas: 'Core business / focus areas',
  targetPopulation: 'Target population',
  programmesSummary: 'Programmes & services',
  distributionChannel: 'Distribution channel',
  fullTimeFemales: 'Full-time females',
  fullTimeMales: 'Full-time males',
  volunteersCount: 'Volunteers',
  isRegisteredNpo: 'Registered NPO?',
  isRegisteredDsd: 'Registered with DSD?',
  beneficiaryRaces: 'Race',
  beneficiaryGenders: 'Gender',
  beneficiaryAgeGroups: 'Age groups',
  feedingFrequency: 'Feeding frequency',
  saHarvestReliance: 'Reliance on SA Harvest',
  transportCapacity: 'Transport capacity',
  totalPeopleServed: 'Total people served',
  femalesServed: 'Females served',
  malesServed: 'Males served',
  africanServed: 'African served',
  colouredServed: 'Coloured served',
  indianAsianServed: 'Indian/Asian served',
  whiteServed: 'White served',
  mealsProvided: 'Meals provided',
  feedingDays: 'Days of the week',
  lastFedDate: 'Last date fed by SA Harvest',
  foodStorageTypes: 'Food storage',
  kitchenClean: 'Kitchen clean and organised?',
  waterAccess: 'Access to water / cleaning products?',
  toiletFacilities: 'Toilets & hygiene facilities?',
  pestFree: 'Pest / rodent free?',
  infrastructureChecks: 'Infrastructure checks (tick those in safe working order)',
  deliveryAccessEase: 'Easy access for delivery?',
  parkingAndSecurity: 'Parking & security?',
  policeStationProximity: 'Proximity to police station',
  officerComments: 'Additional comments',
  proposalCapabilities: 'Proposal writing capabilities',
  digitalCapabilities: 'Digital capabilities',
  slaSigned: 'SLA signed?',
  consentFormSigned: 'Consent form signed?',
  policyDocumentPresent: 'Policy document present?',
};

/** Which section each assessment file slot belongs to (#158 slot route values). */
export const FILE_SLOT_SECTION = {
  'npo-certificate': 'registration',
  'pbo-certificate': 'registration',
  'kitchen-storage-photos': 'facilities',
  'facility-photos': 'capacity',
  certificates: 'documents',
} as const;
