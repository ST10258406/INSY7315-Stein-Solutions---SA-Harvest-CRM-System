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
