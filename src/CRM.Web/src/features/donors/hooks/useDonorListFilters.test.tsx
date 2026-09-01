import { renderHook, act } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { MemoryRouter, useSearchParams } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useDonorListFilters } from './useDonorListFilters';

function wrapperWith(initialEntry: string) {
  return ({ children }: { children: ReactNode }) => <MemoryRouter initialEntries={[initialEntry]}>{children}</MemoryRouter>;
}

function renderWithSearchParams(initialEntry: string) {
  return renderHook(
    () => ({ filters: useDonorListFilters(), searchParams: useSearchParams()[0] }),
    { wrapper: wrapperWith(initialEntry) }
  );
}

describe('useDonorListFilters', () => {
  it('defaults page and pageSize when the URL has no query string', () => {
    const { result } = renderWithSearchParams('/donors');

    expect(result.current.filters.filters).toEqual({
      page: 1,
      pageSize: 20,
      search: undefined,
      status: undefined,
      companyTypeId: undefined,
      regionCode: undefined,
      donationTypeId: undefined,
      donationFrequencyId: undefined,
      followUpBefore: undefined,
      sortBy: undefined,
      sortDir: undefined,
    });
  });

  it('parses filters, sort and pagination from the URL', () => {
    const { result } = renderWithSearchParams(
      '/donors?page=3&pageSize=50&search=Acme&status=Active&companyTypeId=2&regionCode=WC&sortBy=companyName&sortDir=desc'
    );

    expect(result.current.filters.filters).toMatchObject({
      page: 3,
      pageSize: 50,
      search: 'Acme',
      status: 'Active',
      companyTypeId: 2,
      regionCode: 'WC',
      sortBy: 'companyName',
      sortDir: 'desc',
    });
  });

  it('drops an unrecognised status/sortBy rather than passing it through to the API', () => {
    const { result } = renderWithSearchParams('/donors?status=NotAStatus&sortBy=notAField');

    expect(result.current.filters.filters.status).toBeUndefined();
    expect(result.current.filters.filters.sortBy).toBeUndefined();
  });

  it('setFilter writes the param and resets page to 1', () => {
    const { result } = renderWithSearchParams('/donors?page=4&status=Active');

    act(() => result.current.filters.setFilter('status', 'Lapsed'));

    expect(result.current.searchParams.get('status')).toBe('Lapsed');
    expect(result.current.searchParams.get('page')).toBeNull();
  });

  it('setFilter with undefined removes the param', () => {
    const { result } = renderWithSearchParams('/donors?status=Active');

    act(() => result.current.filters.setFilter('status', undefined));

    expect(result.current.searchParams.get('status')).toBeNull();
  });

  it('setSort toggles direction when sorting the same field twice, and resets to asc for a new field', () => {
    const { result } = renderWithSearchParams('/donors');

    act(() => result.current.filters.setSort('companyName'));
    expect(result.current.searchParams.get('sortBy')).toBe('companyName');
    expect(result.current.searchParams.get('sortDir')).toBe('asc');

    act(() => result.current.filters.setSort('companyName'));
    expect(result.current.searchParams.get('sortDir')).toBe('desc');

    act(() => result.current.filters.setSort('followUpDate'));
    expect(result.current.searchParams.get('sortBy')).toBe('followUpDate');
    expect(result.current.searchParams.get('sortDir')).toBe('asc');
  });

  it('setPage updates page without touching other params', () => {
    const { result } = renderWithSearchParams('/donors?status=Active');

    act(() => result.current.filters.setPage(5));

    expect(result.current.searchParams.get('page')).toBe('5');
    expect(result.current.searchParams.get('status')).toBe('Active');
  });

  it('setPageSize updates pageSize and resets page to 1', () => {
    const { result } = renderWithSearchParams('/donors?page=3&pageSize=20');

    act(() => result.current.filters.setPageSize(50));

    expect(result.current.searchParams.get('pageSize')).toBe('50');
    expect(result.current.searchParams.get('page')).toBeNull();
  });

  it('clearFilters wipes every query param', () => {
    const { result } = renderWithSearchParams('/donors?page=3&status=Active&search=Acme');

    act(() => result.current.filters.clearFilters());

    expect(result.current.searchParams.toString()).toBe('');
  });
});
