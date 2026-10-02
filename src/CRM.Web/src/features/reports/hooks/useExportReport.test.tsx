import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { api } from '@/lib/axios';
import { toast } from 'sonner';
import { useExportReport } from './useExportReport';
import type { ReportExportResult } from '../types';

vi.mock('@/lib/axios', () => ({
  api: {
    post: vi.fn(),
  },
}));

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
  },
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
  };
}

/** Stands in for the tab `window.open('', '_blank')` returns — see ReportsExportMenu. */
function createFakeTarget() {
  return { location: { href: '' }, close: vi.fn() } as unknown as Window;
}

const exportResult: ReportExportResult = {
  downloadUrl: 'https://blob.example/reports/donors-contacted/abc/report.pdf?sas=1',
  expiresAt: '2026-09-15T21:00:00Z',
  fileName: 'donors-contacted-2026-09.pdf',
  fileSizeBytes: 2048,
};

describe('useExportReport', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('POSTs reportType/format/filters exactly as given for the donors-contacted report', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: exportResult } });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });

    result.current.mutate({
      reportType: 'donors-contacted',
      format: 'pdf',
      filters: { startDate: '2026-09-01', endDate: '2026-09-30' },
      target: createFakeTarget(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/reports/export', {
      reportType: 'donors-contacted',
      format: 'pdf',
      filters: { startDate: '2026-09-01', endDate: '2026-09-30' },
    });
  });

  it('drops filters for report types other than donors-contacted', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: exportResult } });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });

    result.current.mutate({
      reportType: 'donors-by-region',
      format: 'excel',
      filters: { startDate: '2026-09-01', endDate: '2026-09-30' },
      target: createFakeTarget(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(api.post).toHaveBeenCalledWith('/api/v1/reports/export', {
      reportType: 'donors-by-region',
      format: 'excel',
      filters: undefined,
    });
  });

  it('on success, redirects the pre-opened tab to the returned SAS URL', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: exportResult } });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });
    const target = createFakeTarget();

    result.current.mutate({ reportType: 'donors-by-status', format: 'pdf', target });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(target.location.href).toBe(exportResult.downloadUrl);
  });

  it('on success with no pre-opened tab (popup blocked before the request even started), toasts instead of throwing', async () => {
    vi.mocked(api.post).mockResolvedValueOnce({ data: { data: exportResult } });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });

    result.current.mutate({ reportType: 'donors-by-status', format: 'pdf', target: null });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(toast.error).toHaveBeenCalledWith(
      'Your browser blocked the export tab. Please allow pop-ups for this site and try again.',
    );
  });

  it('on error, closes the pre-opened tab and shows a toast with the backend validation message', async () => {
    vi.mocked(api.post).mockRejectedValueOnce({
      response: { data: { message: 'filters.startDate and filters.endDate are required for the donors-contacted report.' } },
    });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });
    const target = createFakeTarget();

    result.current.mutate({ reportType: 'donors-contacted', format: 'pdf', target });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(target.close).toHaveBeenCalledTimes(1);
    expect(toast.error).toHaveBeenCalledWith(
      'filters.startDate and filters.endDate are required for the donors-contacted report.',
    );
  });

  it('on error with no backend message, falls back to a generic export-failed toast', async () => {
    vi.mocked(api.post).mockRejectedValueOnce(new Error('network down'));
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });

    result.current.mutate({ reportType: 'donors-by-type', format: 'excel', target: createFakeTarget() });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(toast.error).toHaveBeenCalledWith('Export failed. Please try again.');
  });

  it('never reuses a previous downloadUrl — each mutate call is a fresh request', async () => {
    vi.mocked(api.post)
      .mockResolvedValueOnce({ data: { data: exportResult } })
      .mockResolvedValueOnce({ data: { data: { ...exportResult, downloadUrl: 'https://blob.example/second' } } });
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useExportReport(), { wrapper });

    const firstTarget = createFakeTarget();
    result.current.mutate({
      reportType: 'donors-contacted',
      format: 'pdf',
      filters: { startDate: '2026-09-01', endDate: '2026-09-30' },
      target: firstTarget,
    });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    const secondTarget = createFakeTarget();
    result.current.mutate({
      reportType: 'donors-contacted',
      format: 'pdf',
      filters: { startDate: '2026-09-01', endDate: '2026-09-30' },
      target: secondTarget,
    });
    await waitFor(() => expect(api.post).toHaveBeenCalledTimes(2));

    expect(firstTarget.location.href).toBe(exportResult.downloadUrl);
    expect(secondTarget.location.href).toBe('https://blob.example/second');
  });
});
