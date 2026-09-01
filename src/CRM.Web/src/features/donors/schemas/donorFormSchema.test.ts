import { describe, it, expect } from 'vitest';
import { z } from 'zod';
import { createDonorFormSchema, emptyDonorFormValues, type DonorFormValues } from './donorFormSchema';

function validValues(): DonorFormValues {
  return {
    ...emptyDonorFormValues,
    company: {
      companyName: 'Acme Co',
      companyTypeId: '1',
      website: 'https://acme.example',
      registeredCompanyName: 'Acme Co (Pty) Ltd',
      tradingName: 'Acme',
      entityTypeId: '1',
      companyRegistrationNumber: '2020/123456/07',
      incomeTaxNumber: '1234567890',
    },
    primaryContact: { name: 'Jane Doe', jobTitle: 'Ops Manager', phone: '0821234567', email: 'jane@acme.example' },
    legalAddress: {
      streetAddress: '1 Main St',
      suburb: 'CBD',
      city: 'Cape Town',
      provinceId: '1',
      postalCode: '8001',
    },
    donations: {
      frequencyId: '1',
      typeIds: ['1'],
      collectionAddress: '1 Main St, Cape Town',
      operationsLogisticsDetails: '',
      regionIds: ['1'],
    },
  };
}

function issuePaths(result: z.ZodSafeParseResult<unknown>): string[] {
  return result.success ? [] : result.error.issues.map((issue) => issue.path.join('.'));
}

describe('donorFormSchema (create mode)', () => {
  const schema = createDonorFormSchema('create');

  it('accepts a fully valid payload', () => {
    expect(schema.safeParse(validValues()).success).toBe(true);
  });

  // Non-negotiable SA income tax number rule — the frontend layer of the
  // three-level rule shared with FluentValidation (CreateDonorCommandValidator
  // / UpdateDonorCommandValidator) and the chk_donors_tax_number DB constraint.
  it('rejects an income tax number starting with 4', () => {
    const values = validValues();
    values.company.incomeTaxNumber = '4123456789';

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.join('.') === 'company.incomeTaxNumber');
      expect(issue?.message).toBe('Income tax number cannot start with 4');
    }
  });

  it('accepts an income tax number not starting with 4', () => {
    const values = validValues();
    values.company.incomeTaxNumber = '9123456789';
    expect(schema.safeParse(values).success).toBe(true);
  });

  it('requires an income tax number to be present (matches CreateDonorCommandValidator NotEmpty)', () => {
    const values = validValues();
    values.company.incomeTaxNumber = '';
    const result = schema.safeParse(values);
    expect(result.success).toBe(false);
  });

  it('does not require marketing/accounts contact fields when not toggled on', () => {
    expect(schema.safeParse(validValues()).success).toBe(true);
  });

  it('requires name, phone, and email once the marketing contact is toggled on', () => {
    const values = validValues();
    values.hasMarketingContact = true;

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    expect(issuePaths(result)).toEqual(
      expect.arrayContaining(['marketingContact.name', 'marketingContact.phone', 'marketingContact.email'])
    );
  });

  it('validates the marketing contact email format once toggled on', () => {
    const values = validValues();
    values.hasMarketingContact = true;
    values.marketingContact = { name: 'Sam', jobTitle: '', phone: '0821234567', email: 'not-an-email' };

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.join('.') === 'marketingContact.email');
      expect(issue?.message).toBe('Enter a valid email address');
    }
  });

  it('requires at least one donation type and one operational region', () => {
    const values = validValues();
    values.donations.typeIds = [];
    values.donations.regionIds = [];

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    expect(issuePaths(result)).toEqual(expect.arrayContaining(['donations.typeIds', 'donations.regionIds']));
  });

  it('requires the required dropdowns to be selected', () => {
    const values = validValues();
    values.company.companyTypeId = '';
    values.legalAddress.provinceId = '';

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    expect(issuePaths(result)).toEqual(expect.arrayContaining(['company.companyTypeId', 'legalAddress.provinceId']));
  });

  it('requires website, registered company name, trading name, and company registration number', () => {
    const values = validValues();
    values.company.website = '';
    values.company.registeredCompanyName = '';
    values.company.tradingName = '';
    values.company.companyRegistrationNumber = '';

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    expect(issuePaths(result)).toEqual(
      expect.arrayContaining([
        'company.website',
        'company.registeredCompanyName',
        'company.tradingName',
        'company.companyRegistrationNumber',
      ])
    );
  });
});

describe('donorFormSchema (edit mode)', () => {
  const schema = createDonorFormSchema('edit');

  // UpdateDonorCommandValidator only requires these fields NotEmpty when
  // present, i.e. they're optional on a PATCH — an edit shouldn't be blocked
  // by a legacy donor record missing one of these.
  it('allows website, registered company name, trading name, and company registration number to be blank', () => {
    const values = validValues();
    values.company.website = '';
    values.company.registeredCompanyName = '';
    values.company.tradingName = '';
    values.company.companyRegistrationNumber = '';

    expect(schema.safeParse(values).success).toBe(true);
  });

  it('allows an empty income tax number', () => {
    const values = validValues();
    values.company.incomeTaxNumber = '';
    expect(schema.safeParse(values).success).toBe(true);
  });

  it('still rejects an income tax number starting with 4', () => {
    const values = validValues();
    values.company.incomeTaxNumber = '4123456789';

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.join('.') === 'company.incomeTaxNumber');
      expect(issue?.message).toBe('Income tax number cannot start with 4');
    }
  });

  it('still requires the id-backed dropdowns and donation type/region selections', () => {
    const values = validValues();
    values.company.companyTypeId = '';
    values.donations.typeIds = [];

    const result = schema.safeParse(values);

    expect(result.success).toBe(false);
    expect(issuePaths(result)).toEqual(expect.arrayContaining(['company.companyTypeId', 'donations.typeIds']));
  });
});
