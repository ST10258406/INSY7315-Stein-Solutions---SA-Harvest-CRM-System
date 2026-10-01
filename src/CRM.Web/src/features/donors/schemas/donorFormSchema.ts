// src/features/donors/schemas/donorFormSchema.ts
import { z } from 'zod';

export type DonorFormMode = 'create' | 'edit';

// Every id-backed dropdown is kept as a string in form state (native <select>
// values are always strings) and converted to a number only at the request-
// mapping boundary — see donorFormMapping.ts.

const requiredIdSchema = (message: string) => z.string().trim().min(1, message);

// A handful of Company/LegalAddress/Donations scalar fields are NotEmpty on
// CreateDonorCommandValidator but only conditionally required (i.e. optional)
// on UpdateDonorCommandValidator — PATCH semantics let an edit omit/blank a
// field that was already required at creation (e.g. legacy data). Mirror that
// asymmetry here instead of forcing edit-mode donors through create's
// stricter rules.
const requiredOnCreate = (mode: DonorFormMode, message: string) =>
  mode === 'create' ? z.string().trim().min(1, message) : z.string().trim();

function buildCompanySchema(mode: DonorFormMode) {
  return z.object({
    companyName: z.string().trim().min(1, 'Company name is required'),
    companyTypeId: requiredIdSchema('Select a company type'),
    website: requiredOnCreate(mode, 'Website is required'),
    registeredCompanyName: requiredOnCreate(mode, 'Registered company name is required'),
    tradingName: requiredOnCreate(mode, 'Trading name is required'),
    entityTypeId: requiredIdSchema('Select an entity type'),
    companyRegistrationNumber: requiredOnCreate(mode, 'Company registration number is required'),
    // Non-negotiable SA income tax number rule: must not start with '4'. This
    // is the frontend layer of a three-level rule also enforced by
    // FluentValidation (CreateDonorCommandValidator / UpdateDonorCommandValidator)
    // and a DB CHECK constraint (chk_donors_tax_number). Do not remove this
    // refine — required-ness (NotEmpty on create only) is separate from it.
    incomeTaxNumber: requiredOnCreate(mode, 'Income tax number is required').refine(
      (val) => !val || !val.startsWith('4'),
      { message: 'Income tax number cannot start with 4' }
    ),
  });
}

const requiredContactSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  jobTitle: z.string().trim().optional(),
  phone: z.string().trim().min(1, 'Phone is required'),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
});

// Marketing/accounts contacts are optional as a whole (toggled on in the UI),
// but once included, the same required-field rules apply as the primary
// contact — enforced by the root-level superRefine below rather than here,
// since "required" depends on the sibling hasXContact flag.
const optionalContactSchema = z.object({
  name: z.string().trim().optional(),
  jobTitle: z.string().trim().optional(),
  phone: z.string().trim().optional(),
  email: z.string().trim().optional(),
});

function buildLegalAddressSchema(mode: DonorFormMode) {
  return z.object({
    streetAddress: requiredOnCreate(mode, 'Street address is required'),
    suburb: requiredOnCreate(mode, 'Suburb is required'),
    city: requiredOnCreate(mode, 'City is required'),
    provinceId: requiredIdSchema('Select a province'),
    postalCode: requiredOnCreate(mode, 'Postal code is required'),
  });
}

function buildDonationsSchema(mode: DonorFormMode) {
  return z.object({
    frequencyId: requiredIdSchema('Select a donation frequency'),
    typeIds: z.array(z.string()).min(1, 'Select at least one donation type'),
    collectionAddress: requiredOnCreate(mode, 'Collection address is required'),
    operationsLogisticsDetails: z.string().trim().optional(),
    regionIds: z.array(z.string()).min(1, 'Select at least one operational region'),
  });
}

export function createDonorFormSchema(mode: DonorFormMode) {
  return z
    .object({
      company: buildCompanySchema(mode),
      primaryContact: requiredContactSchema,
      hasMarketingContact: z.boolean(),
      marketingContact: optionalContactSchema,
      hasAccountsContact: z.boolean(),
      accountsContact: optionalContactSchema,
      legalAddress: buildLegalAddressSchema(mode),
      donations: buildDonationsSchema(mode),
      bbbeeStatusId: z.string().trim().optional(),
      relationshipManagerId: z.string().trim().optional(),
      marketingConsent: z.boolean(),
      impactReportingPreferences: z.string().trim().optional(),
      additionalInformation: z.string().trim().optional(),
    })
    .superRefine((values, ctx) => {
      if (values.hasMarketingContact) {
        validateOptionalContact(values.marketingContact, ['marketingContact'], ctx);
      }
      if (values.hasAccountsContact) {
        validateOptionalContact(values.accountsContact, ['accountsContact'], ctx);
      }
    });
}

function validateOptionalContact(
  contact: { name?: string; phone?: string; email?: string },
  basePath: string[],
  ctx: z.RefinementCtx
) {
  if (!contact.name) {
    ctx.addIssue({ code: 'custom', path: [...basePath, 'name'], message: 'Name is required' });
  }
  if (!contact.phone) {
    ctx.addIssue({ code: 'custom', path: [...basePath, 'phone'], message: 'Phone is required' });
  }
  if (!contact.email) {
    ctx.addIssue({ code: 'custom', path: [...basePath, 'email'], message: 'Email is required' });
  } else if (!z.string().email().safeParse(contact.email).success) {
    ctx.addIssue({ code: 'custom', path: [...basePath, 'email'], message: 'Enter a valid email address' });
  }
}

// Field shape is identical between modes (every field is a plain string /
// string[] / boolean — see requiredOnCreate above); only strictness differs.
export type DonorFormValues = z.infer<ReturnType<typeof createDonorFormSchema>>;

export const emptyContactValues = { name: '', jobTitle: '', phone: '', email: '' };

export const emptyDonorFormValues: DonorFormValues = {
  company: {
    companyName: '',
    companyTypeId: '',
    website: '',
    registeredCompanyName: '',
    tradingName: '',
    entityTypeId: '',
    companyRegistrationNumber: '',
    incomeTaxNumber: '',
  },
  primaryContact: { ...emptyContactValues },
  hasMarketingContact: false,
  marketingContact: { ...emptyContactValues },
  hasAccountsContact: false,
  accountsContact: { ...emptyContactValues },
  legalAddress: {
    streetAddress: '',
    suburb: '',
    city: '',
    provinceId: '',
    postalCode: '',
  },
  donations: {
    frequencyId: '',
    typeIds: [],
    collectionAddress: '',
    operationsLogisticsDetails: '',
    regionIds: [],
  },
  bbbeeStatusId: '',
  relationshipManagerId: '',
  marketingConsent: false,
  impactReportingPreferences: '',
  additionalInformation: '',
};
