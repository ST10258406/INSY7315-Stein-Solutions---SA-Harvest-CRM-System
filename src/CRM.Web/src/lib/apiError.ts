export interface ApiErrorDetail {
  field: string;
  message: string;
}

export interface ApiErrorEnvelope {
  status: number;
  code: string;
  message: string;
  errors: ApiErrorDetail[] | null;
  traceId: string;
}

interface AxiosLikeError {
  isAxiosError: true;
  response?: { data?: unknown };
}

function isAxiosLikeError(error: unknown): error is AxiosLikeError {
  return typeof error === 'object' && error !== null && (error as { isAxiosError?: unknown }).isAxiosError === true;
}

/**
 * Looks up a field-level message from the backend's VALIDATION_ERROR envelope
 * (see ExceptionHandlingMiddleware), e.g. an invalid/expired reset token surfaces
 * as a "Token" field error rather than a distinct HTTP status.
 */
export function getFieldError(error: unknown, field: string): string | undefined {
  if (!isAxiosLikeError(error)) return undefined;
  const data = error.response?.data as Partial<ApiErrorEnvelope> | undefined;
  return data?.errors?.find((e) => e.field.toLowerCase() === field.toLowerCase())?.message;
}
