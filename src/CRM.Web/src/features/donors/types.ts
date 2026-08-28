import type { AxiosError } from 'axios';
import type { ApiErrorEnvelope } from '@/lib/apiError';

/** Error shape thrown by every donor hook — matches the backend's standard envelope. */
export type ApiError = AxiosError<ApiErrorEnvelope>;

export type DonorStatus = 'PendingReview' | 'Active' | 'Lapsed' | 'Rejected';

export type DonorSortField = 'followUpDate' | 'companyName' | 'createdAt' | 'lastInteractionDate';

// Mirrors GetDonorsQuery (CRM.Application.Modules.Donors.Queries.GetDonors) exactly —
// keep these two in sync.
export interface DonorFilters {
  page?: number;
  pageSize?: number;
  sortBy?: DonorSortField;
  sortDir?: 'asc' | 'desc';
  search?: string;
  status?: DonorStatus;
  companyTypeId?: number;
  regionCode?: string;
  donationTypeId?: number;
  donationFrequencyId?: number;
  relationshipManagerId?: string;
  followUpBefore?: string;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PaginatedResult<T> {
  data: T[];
  pagination: PaginationMeta;
}

export interface RelationshipManagerDto {
  id: string;
  fullName: string;
}

export interface DonorListItemDto {
  id: string;
  companyName: string;
  companyType: string;
  status: string;
  submissionSource: string;
  relationshipManager: RelationshipManagerDto | null;
  followUpDate: string | null;
  lastInteractionDate: string | null;
  lastInteractionType: string | null;
  operationalRegions: string[];
  donationFrequency: string | null;
  donationTypes: string[];
}

export interface LookupDto {
  id: number;
  name: string;
}

export interface ProvinceDto {
  id: number;
  code: string;
  name: string;
}

export interface RegionDto {
  id: number;
  code: string;
  name: string;
}

export interface DonorCompanyDto {
  companyName: string;
  companyType: LookupDto;
  website: string | null;
  registeredCompanyName: string;
  tradingName: string | null;
  entityType: LookupDto;
  companyRegistrationNumber: string | null;
  incomeTaxNumber: string | null;
}

export interface DonorContactDto {
  name: string;
  jobTitle: string | null;
  phone: string | null;
  email: string | null;
}

export interface DonorLegalAddressDto {
  streetNameNumber: string;
  suburb: string;
  city: string;
  province: ProvinceDto;
  postalCode: string;
}

export interface DonorDonationsDto {
  frequency: LookupDto;
  types: LookupDto[];
  collectionAddress: string | null;
  operationsLogisticsDetails: string | null;
  operationalRegions: RegionDto[];
}

export interface DonorDocumentDto {
  id: string;
  documentType: string;
  originalFileName: string;
  fileSizeBytes: number;
  mimeType: string;
  uploadedAt: string;
  isActive: boolean;
}

export interface DonorComplianceDto {
  bbbeeStatus: LookupDto | null;
  documents: DonorDocumentDto[];
}

export interface DonorCrmDto {
  relationshipManager: RelationshipManagerDto | null;
  marketingConsent: boolean;
  marketingConsentDate: string | null;
  impactReportingPreferences: string | null;
  followUpDate: string | null;
  additionalInformation: string | null;
}

export interface DonorDetailDto {
  id: string;
  status: string;
  submissionSource: string;
  foodspaceCompanyId: string | null;
  createdAt: string;
  updatedAt: string;
  company: DonorCompanyDto;
  primaryContact: DonorContactDto;
  marketingContact: DonorContactDto | null;
  accountsContact: DonorContactDto | null;
  legalAddress: DonorLegalAddressDto | null;
  donations: DonorDonationsDto;
  compliance: DonorComplianceDto;
  crm: DonorCrmDto;
}

export interface CreateDonorCompanyRequest {
  companyName: string;
  companyTypeId: number;
  website?: string | null;
  registeredCompanyName: string;
  tradingName?: string | null;
  entityTypeId: number;
  companyRegistrationNumber?: string | null;
  incomeTaxNumber?: string | null;
}

export interface CreateDonorContactRequest {
  name: string;
  jobTitle?: string | null;
  phone?: string | null;
  email?: string | null;
}

export interface CreateDonorLegalAddressRequest {
  streetAddress: string;
  suburb: string;
  city: string;
  provinceId: number;
  postalCode: string;
}

export interface CreateDonorDonationsRequest {
  frequencyId: number;
  typeIds: number[];
  collectionAddress: string;
  operationsLogisticsDetails?: string | null;
  regionIds: number[];
}

export interface CreateDonorComplianceRequest {
  bbbeeStatusId?: number | null;
}

export interface CreateDonorCrmRequest {
  relationshipManagerId?: string | null;
  marketingConsent: boolean;
  impactReportingPreferences?: string | null;
  additionalInformation?: string | null;
}

// Mirrors CreateDonorRequest (CRM.Application.Modules.Donors.Dtos) — no id/status/
// submissionSource/createdAt/updatedAt, those are server-set.
export interface CreateDonorRequest {
  company: CreateDonorCompanyRequest;
  primaryContact: CreateDonorContactRequest;
  marketingContact?: CreateDonorContactRequest | null;
  accountsContact?: CreateDonorContactRequest | null;
  legalAddress: CreateDonorLegalAddressRequest;
  donations: CreateDonorDonationsRequest;
  compliance?: CreateDonorComplianceRequest | null;
  crm?: CreateDonorCrmRequest | null;
}

export interface UpdateDonorCompanyRequest {
  companyName?: string;
  companyTypeId?: number;
  website?: string | null;
  registeredCompanyName?: string;
  tradingName?: string | null;
  entityTypeId?: number;
  companyRegistrationNumber?: string | null;
  incomeTaxNumber?: string | null;
}

export interface UpdateDonorContactRequest {
  name?: string;
  jobTitle?: string | null;
  phone?: string | null;
  email?: string | null;
}

export interface UpdateDonorLegalAddressRequest {
  streetAddress?: string;
  suburb?: string;
  city?: string;
  provinceId?: number;
  postalCode?: string;
}

export interface UpdateDonorDonationsRequest {
  frequencyId?: number;
  typeIds?: number[];
  collectionAddress?: string;
  operationsLogisticsDetails?: string | null;
  regionIds?: number[];
}

export interface UpdateDonorComplianceRequest {
  bbbeeStatusId?: number | null;
}

export interface UpdateDonorCrmRequest {
  relationshipManagerId?: string | null;
  marketingConsent?: boolean;
  impactReportingPreferences?: string | null;
  additionalInformation?: string | null;
}

// Mirrors UpdateDonorRequest — a null/omitted section means "untouched"; a present
// section with a null sub-field means that sub-field is untouched. Deliberately no
// status property (see backend comment: that's the Approvals workflow's job).
export interface UpdateDonorRequest {
  company?: UpdateDonorCompanyRequest;
  primaryContact?: UpdateDonorContactRequest;
  marketingContact?: UpdateDonorContactRequest;
  accountsContact?: UpdateDonorContactRequest;
  legalAddress?: UpdateDonorLegalAddressRequest;
  donations?: UpdateDonorDonationsRequest;
  compliance?: UpdateDonorComplianceRequest;
  crm?: UpdateDonorCrmRequest;
}
