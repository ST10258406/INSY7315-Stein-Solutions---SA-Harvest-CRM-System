import axios from 'axios';

// A donor filling out the public form has no session — this deliberately
// skips `api`'s auth-header/refresh-on-401 interceptors (lib/axios.ts) so an
// unexpected 401/expired-token response can never trigger a redirect to
// /login for someone who was never logged in.
export const publicApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});
