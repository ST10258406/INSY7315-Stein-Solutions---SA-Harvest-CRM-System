import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/authStore';
import { paths } from '@/routes/paths';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL, // e.g. http://localhost:5000/api/v1
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const accessToken = useAuthStore.getState().accessToken;

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }

  return config;
});

let isRefreshing = false;
let refreshSubscribers: Array<(token: string | null) => void> = [];

function subscribeTokenRefresh(callback: (token: string | null) => void) {
  refreshSubscribers.push(callback);
}

function onRefreshed(token: string | null) {
  refreshSubscribers.forEach((callback) => callback(token));
  refreshSubscribers = [];
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig;

    // Only try to recover from 401s, and only once per request.
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Don't try to "refresh" the refresh call itself — that's the loop guard.
    if (originalRequest.url?.includes('/auth/refresh')) {
      useAuthStore.getState().logout();
      window.location.href = paths.login;
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    if (isRefreshing) {
      // A refresh is already in flight (e.g. two requests 401'd at once) —
      // queue this request instead of firing a second refresh call.
      return new Promise((resolve, reject) => {
        subscribeTokenRefresh((newToken: string | null) => {
          if (newToken) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            resolve(api(originalRequest));
          } else {
            reject(new Error('Token refresh failed'));
          }
        });
      });
    }

    isRefreshing = true;

    try {
      const refreshToken = useAuthStore.getState().refreshToken;
      const { data } = await axios.post(
        `${import.meta.env.VITE_API_BASE_URL}/api/auth/refresh`,
        { refreshToken }
      );

      useAuthStore.getState().setAccessToken(data.accessToken);
      onRefreshed(data.accessToken);

      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      return api(originalRequest);
    } catch (refreshError) {
      onRefreshed(null); // Reject any queued requests
      useAuthStore.getState().logout();
      window.location.href = paths.login;
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

