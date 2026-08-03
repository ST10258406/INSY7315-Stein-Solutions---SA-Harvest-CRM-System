export interface UserSummaryDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
}

export interface LoginResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserSummaryDto;
}

export interface ForgotPasswordResponseDto {
  message: string;
}

export interface ResetPasswordResponseDto {
  message: string;
}

export interface DecodedJwt {
  header: Record<string, unknown>;
  payload: Record<string, unknown>;
}

/**
 * Decodes a base64-encoded JWT token string into header and payload objects.
 */
export function decodeJwtToken(token: string): DecodedJwt {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid JWT format: Token must contain 3 parts');
    }
    const base64UrlDecode = (str: string) => {
      let base64 = str.replace(/-/g, '+').replace(/_/g, '/');
      while (base64.length % 4 !== 0) {
        base64 += '=';
      }
      return JSON.parse(atob(base64));
    };

    return {
      header: base64UrlDecode(parts[0]),
      payload: base64UrlDecode(parts[1]),
    };
  } catch (err) {
    console.error('Failed to decode JWT token:', err);
    return {
      header: { alg: 'Unknown', typ: 'JWT' },
      payload: { error: 'Failed to decode payload' },
    };
  }
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

/**
 * Authenticates user credentials directly against the CRM API backend (POST /api/auth/login).
 * The frontend communicates strictly via HTTP REST endpoints and does NOT access the database directly.
 */
export async function loginApi(email: string, password: string): Promise<LoginResponseDto> {
  const url = `${API_BASE_URL}/api/auth/login`;

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    });

    if (response.status === 401) {
      throw new Error('Invalid email or password. Please verify your credentials.');
    }

    if (!response.ok) {
      let errorMessage = `Server error (${response.status})`;
      try {
        const errorData = await response.json();
        if (errorData?.detail) {
          errorMessage = errorData.detail;
        } else if (errorData?.title) {
          errorMessage = errorData.title;
        } else if (typeof errorData === 'string') {
          errorMessage = errorData;
        }
      } catch {
        // If response body is not JSON
      }
      throw new Error(errorMessage);
    }

    const data: LoginResponseDto = await response.json();
    return data;
  } catch (err: unknown) {
    if (err instanceof Error && err.message) {
      throw err;
    }
    throw new Error('Unable to connect to CRM API. Please ensure the backend server is running on http://localhost:5278.', { cause: err });
  }
}
