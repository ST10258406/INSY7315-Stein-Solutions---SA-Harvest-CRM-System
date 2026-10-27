// TypeScript mirrors of the vetting API DTOs (#156, #157, #158, #159). Vetting targets donors:
// every submission and site visit belongs to a donor. Enum values arrive as their C# names.

export type SiteVisitStatus = 'ToDo' | 'InProgress' | 'Completed';
export type VettingApprovalState = 'New' | 'Ready' | 'Returned' | 'Approved' | 'Declined';
export type VettingDecision = 'Approve' | 'Return' | 'Decline';
export type VettingFlagSource = 'Auto' | 'Reviewer';
export type EvidencePhotoCategory = 'Kitchen' | 'DryStorage' | 'FridgeFreezer' | 'ToiletsWater';
export type VettingFileSlot = 'npo-certificate' | 'pbo-certificate' | 'kitchen-storage-photos' | 'facility-photos' | 'certificates';

// ---- Officers (#156)
export interface VettingOfficer {
  id: string;
  fullName: string;
  initials: string;
  assignedRegion: string;
  workloadCount: number;
}

export interface CreateOfficerRequest {
  fullName: string;
  initials: string;
  assignedRegion: string;
}

export type UpdateOfficerRequest = Partial<CreateOfficerRequest>;

// ---- Site visits (#156)
export interface VettingSiteVisit {
  id: string;
  donorId: string;
  donorCompanyName: string;
  officerId: string;
  officerInitials: string;
  scheduledDate: string; // yyyy-MM-dd
  scheduledTime: string; // HH:mm:ss
  locationLabel: string;
  visitNotes: string | null;
  visitStatus: SiteVisitStatus;
}

export interface CreateSiteVisitRequest {
  donorId: string;
  officerId: string;
  scheduledDate: string;
  scheduledTime: string;
  /** Defaults to the donor's legal-address suburb when omitted. */
  locationLabel?: string;
  visitNotes?: string;
}

export interface UpdateSiteVisitRequest {
  scheduledDate?: string;
  scheduledTime?: string;
  locationLabel?: string;
  visitNotes?: string;
  visitStatus?: SiteVisitStatus;
  officerId?: string;
}

// ---- Submissions (#157)
/** The 11 assessment sections. Booleans are null until answered; blob paths are never exposed. */
export interface VettingAssessmentDetails {
  // 1 Organisation & Contact
  legalName: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  website: string | null;
  // 2 Location
  addressPhysical: string | null;
  addressDetail: string | null;
  province: string;
  what3Words: string;
  // 3 Programmes & Services
  coreFocusAreas: string[];
  targetPopulation: string[];
  programmesSummary: string;
  distributionChannel: string;
  // 4 Staffing
  fullTimeFemales: number;
  fullTimeMales: number;
  volunteersCount: number;
  // 5 Registration
  isRegisteredNpo: boolean | null;
  isRegisteredDsd: boolean | null;
  // 6 Demographics
  beneficiaryRaces: string[];
  beneficiaryGenders: string[];
  beneficiaryAgeGroups: string[];
  // 7 Feeding Operation
  feedingFrequency: string;
  totalPeopleServed: number;
  femalesServed: number;
  malesServed: number;
  africanServed: number;
  colouredServed: number;
  indianAsianServed: number;
  whiteServed: number;
  saHarvestReliance: string;
  transportCapacity: string;
  mealsProvided: string[];
  feedingDays: string[];
  lastFedDate: string | null; // yyyy-MM-dd
  // 8 Facilities & Hygiene
  foodStorageTypes: string[];
  kitchenClean: boolean | null;
  waterAccess: boolean | null;
  toiletFacilities: boolean | null;
  pestFree: boolean | null;
  infrastructureChecks: string[];
  // 9 Access & Security
  deliveryAccessEase: boolean | null;
  parkingAndSecurity: boolean | null;
  policeStationProximity: string | null;
  // 10 Capacity Notes
  officerComments: string | null;
  proposalCapabilities: string | null;
  digitalCapabilities: string | null;
  // 11 Documents
  slaSigned: boolean | null;
  consentFormSigned: boolean | null;
  policyDocumentPresent: boolean | null;
}

export interface VettingFlag {
  id: string;
  description: string;
  source: VettingFlagSource;
  createdAt: string;
}

export interface VettingPhoto {
  shotIndex: number;
  category: EvidencePhotoCategory;
  isCaptured: boolean;
  fileName: string | null;
  capturedAt: string | null;
}

/** Which of the 5 assessment file slots are filled. */
export interface VettingFiles {
  npoCertificate: boolean;
  pboCertificate: boolean;
  kitchenStoragePhotos: boolean;
  facilityPhotos: boolean;
  certificates: boolean;
}

export interface VettingSubmissionListItem {
  id: string;
  formReference: string;
  donorCompanyName: string;
  officerFullName: string;
  /** The province recorded in the assessment. */
  province: string;
  approvalState: VettingApprovalState;
  submittedAt: string | null;
  activeFlagCount: number;
}

export interface VettingSubmission {
  id: string;
  formReference: string;
  donorId: string;
  donorCompanyName: string;
  officerId: string;
  officerFullName: string;
  siteVisitId: string | null;
  approvalState: VettingApprovalState;
  submittedAt: string | null;
  adminDecision: VettingDecision | null;
  decisionReason: string | null;
  createdAt: string;
  updatedAt: string;
  details: VettingAssessmentDetails;
  flags: VettingFlag[];
  photos: VettingPhoto[];
  files: VettingFiles;
}

export interface CreateSubmissionRequest {
  donorId: string;
  officerId: string;
  siteVisitId?: string;
  details: VettingAssessmentDetails;
}

export interface UpdateSubmissionRequest {
  officerId?: string;
  details: VettingAssessmentDetails;
}

export interface CreatedSubmission {
  id: string;
  formReference: string;
}

export interface VettingSubmissionFilters {
  page?: number;
  pageSize?: number;
  approvalState?: VettingApprovalState;
  donorId?: string;
  officerId?: string;
  search?: string;
  /** Only submittedAt is supported; asc = longest-waiting first (the review queue). */
  sortBy?: 'submittedAt';
  sortDir?: 'asc' | 'desc';
}

// ---- Uploads (#158) — same shape as donor document downloads
export interface VettingDownloadUrl {
  downloadUrl: string;
  expiresAt: string;
  originalFileName: string;
}

// ---- Review (#159)
export interface DecideSubmissionRequest {
  decision: VettingDecision;
  /** Required for Return and Decline. */
  reason?: string;
}
