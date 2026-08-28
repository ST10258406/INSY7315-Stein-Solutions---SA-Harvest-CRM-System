// src/features/donors/schemas/donorFormMapping.ts
import type { CreateDonorRequest, DonorContactDto, DonorDetailDto, UpdateDonorRequest } from '../types';
import { emptyContactValues, type DonorFormValues } from './donorFormSchema';

function toContactValues(contact: DonorContactDto) {
  return {
    name: contact.name,
    jobTitle: contact.jobTitle ?? '',
    phone: contact.phone ?? '',
    email: contact.email ?? '',
  };
}

/** Builds the form's initial values from an existing donor, for the edit screen. */
export function donorToFormValues(donor: DonorDetailDto): DonorFormValues {
  return {
    company: {
      companyName: donor.company.companyName,
      companyTypeId: String(donor.company.companyType.id),
      website: donor.company.website ?? '',
      registeredCompanyName: donor.company.registeredCompanyName,
      tradingName: donor.company.tradingName ?? '',
      entityTypeId: String(donor.company.entityType.id),
      companyRegistrationNumber: donor.company.companyRegistrationNumber ?? '',
      incomeTaxNumber: donor.company.incomeTaxNumber ?? '',
    },
    primaryContact: toContactValues(donor.primaryContact),
    hasMarketingContact: donor.marketingContact !== null,
    marketingContact: donor.marketingContact ? toContactValues(donor.marketingContact) : { ...emptyContactValues },
    hasAccountsContact: donor.accountsContact !== null,
    accountsContact: donor.accountsContact ? toContactValues(donor.accountsContact) : { ...emptyContactValues },
    legalAddress: donor.legalAddress
      ? {
          streetAddress: donor.legalAddress.streetNameNumber,
          suburb: donor.legalAddress.suburb,
          city: donor.legalAddress.city,
          provinceId: String(donor.legalAddress.province.id),
          postalCode: donor.legalAddress.postalCode,
        }
      : { streetAddress: '', suburb: '', city: '', provinceId: '', postalCode: '' },
    donations: {
      frequencyId: String(donor.donations.frequency.id),
      typeIds: donor.donations.types.map((type) => String(type.id)),
      collectionAddress: donor.donations.collectionAddress ?? '',
      operationsLogisticsDetails: donor.donations.operationsLogisticsDetails ?? '',
      regionIds: donor.donations.operationalRegions.map((region) => String(region.id)),
    },
    bbbeeStatusId: donor.compliance.bbbeeStatus ? String(donor.compliance.bbbeeStatus.id) : '',
    // Shell field — see donorFormSchema.ts. Not populated from the existing
    // donor's relationshipManager until Issue 45's lookup lands.
    relationshipManagerId: '',
    marketingConsent: donor.crm.marketingConsent,
    impactReportingPreferences: donor.crm.impactReportingPreferences ?? '',
    additionalInformation: donor.crm.additionalInformation ?? '',
  };
}

function contactRequest(contact: { name?: string; jobTitle?: string; phone?: string; email?: string }) {
  return {
    name: (contact.name ?? '').trim(),
    jobTitle: contact.jobTitle?.trim() || null,
    phone: (contact.phone ?? '').trim(),
    email: (contact.email ?? '').trim(),
  };
}

// The form always submits the full record on both create and edit (see
// DonorForm.tsx) rather than diffing changed fields, so these two mappers
// share the same field-by-field logic — they only differ in how an omitted
// optional section is represented (null for Create's DTOs vs. undefined for
// Update's "omitted = untouched" PATCH semantics).

export function formValuesToCreateRequest(values: DonorFormValues): CreateDonorRequest {
  return {
    company: {
      companyName: values.company.companyName.trim(),
      companyTypeId: Number(values.company.companyTypeId),
      website: values.company.website.trim() || null,
      registeredCompanyName: values.company.registeredCompanyName.trim(),
      tradingName: values.company.tradingName.trim() || null,
      entityTypeId: Number(values.company.entityTypeId),
      companyRegistrationNumber: values.company.companyRegistrationNumber.trim() || null,
      incomeTaxNumber: values.company.incomeTaxNumber?.trim() || null,
    },
    primaryContact: contactRequest(values.primaryContact),
    marketingContact: values.hasMarketingContact ? contactRequest(values.marketingContact) : null,
    accountsContact: values.hasAccountsContact ? contactRequest(values.accountsContact) : null,
    legalAddress: {
      streetAddress: values.legalAddress.streetAddress.trim(),
      suburb: values.legalAddress.suburb.trim(),
      city: values.legalAddress.city.trim(),
      provinceId: Number(values.legalAddress.provinceId),
      postalCode: values.legalAddress.postalCode.trim(),
    },
    donations: {
      frequencyId: Number(values.donations.frequencyId),
      typeIds: values.donations.typeIds.map(Number),
      collectionAddress: values.donations.collectionAddress.trim(),
      operationsLogisticsDetails: values.donations.operationsLogisticsDetails?.trim() || null,
      regionIds: values.donations.regionIds.map(Number),
    },
    compliance: {
      bbbeeStatusId: values.bbbeeStatusId ? Number(values.bbbeeStatusId) : null,
    },
    crm: {
      // Shell field — see donorFormSchema.ts; wired up once Issue 45 ships.
      relationshipManagerId: null,
      marketingConsent: values.marketingConsent,
      impactReportingPreferences: values.impactReportingPreferences?.trim() || null,
      additionalInformation: values.additionalInformation?.trim() || null,
    },
  };
}

export function formValuesToUpdateRequest(values: DonorFormValues): UpdateDonorRequest {
  return {
    company: {
      companyName: values.company.companyName.trim(),
      companyTypeId: Number(values.company.companyTypeId),
      website: values.company.website.trim() || null,
      registeredCompanyName: values.company.registeredCompanyName.trim(),
      tradingName: values.company.tradingName.trim() || null,
      entityTypeId: Number(values.company.entityTypeId),
      companyRegistrationNumber: values.company.companyRegistrationNumber.trim() || null,
      incomeTaxNumber: values.company.incomeTaxNumber?.trim() || null,
    },
    primaryContact: contactRequest(values.primaryContact),
    marketingContact: values.hasMarketingContact ? contactRequest(values.marketingContact) : undefined,
    accountsContact: values.hasAccountsContact ? contactRequest(values.accountsContact) : undefined,
    legalAddress: {
      streetAddress: values.legalAddress.streetAddress.trim(),
      suburb: values.legalAddress.suburb.trim(),
      city: values.legalAddress.city.trim(),
      provinceId: Number(values.legalAddress.provinceId),
      postalCode: values.legalAddress.postalCode.trim(),
    },
    donations: {
      frequencyId: Number(values.donations.frequencyId),
      typeIds: values.donations.typeIds.map(Number),
      collectionAddress: values.donations.collectionAddress.trim(),
      operationsLogisticsDetails: values.donations.operationsLogisticsDetails?.trim() || null,
      regionIds: values.donations.regionIds.map(Number),
    },
    compliance: {
      bbbeeStatusId: values.bbbeeStatusId ? Number(values.bbbeeStatusId) : null,
    },
    crm: {
      relationshipManagerId: null,
      marketingConsent: values.marketingConsent,
      impactReportingPreferences: values.impactReportingPreferences?.trim() || null,
      additionalInformation: values.additionalInformation?.trim() || null,
    },
  };
}
