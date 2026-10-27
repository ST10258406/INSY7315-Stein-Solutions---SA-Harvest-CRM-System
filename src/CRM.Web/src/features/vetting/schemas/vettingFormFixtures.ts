import type { VettingFormValues } from './vettingFormSchema';

/** A complete, valid form (test fixture). */
export function validFormValues(): VettingFormValues {
  return {
    donorId: 'd1',
    officerId: 'o1',
    siteVisitId: '',
    details: {
      legalName: 'Ubuntu Food Co (Pty) Ltd', contactName: 'Sipho Dlamini', contactEmail: 'sipho@example.com',
      contactPhone: '+27 82 000 0000', website: 'https://ubuntu.example.com',
      addressPhysical: '', addressDetail: '', province: 'Gauteng', what3Words: '///apple.orange.banana',
      coreFocusAreas: ['Feeding'], targetPopulation: ['Children'], programmesSummary: 'Daily soup kitchen.',
      distributionChannel: 'Both',
      fullTimeFemales: 2, fullTimeMales: 1, volunteersCount: 5,
      isRegisteredNpo: 'false', isRegisteredDsd: 'true',
      beneficiaryRaces: ['African'], beneficiaryGenders: ['Female', 'Male'], beneficiaryAgeGroups: ['0–5', '60+'],
      feedingFrequency: 'Weekdays only', totalPeopleServed: 50, femalesServed: 30, malesServed: 20,
      africanServed: 50, colouredServed: 0, indianAsianServed: 0, whiteServed: 0,
      saHarvestReliance: 'High', transportCapacity: 'None — delivery required',
      mealsProvided: ['Lunch'], feedingDays: ['Mon', 'Fri'], lastFedDate: '2026-01-15',
      foodStorageTypes: ['Dry'], kitchenClean: 'true', waterAccess: 'true', toiletFacilities: 'false', pestFree: 'true',
      infrastructureChecks: [],
      deliveryAccessEase: 'true', parkingAndSecurity: 'true', policeStationProximity: '2 km',
      officerComments: '', proposalCapabilities: '', digitalCapabilities: '',
      slaSigned: 'true', consentFormSigned: 'true', policyDocumentPresent: 'true',
    },
  };
}
