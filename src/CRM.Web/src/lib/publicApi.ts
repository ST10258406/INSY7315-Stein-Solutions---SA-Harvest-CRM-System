// A donor filling out the public form has no session — this deliberately
// skips `api`'s auth-header/refresh-on-401 interceptors (lib/axios.ts) so an
// unexpected 401/expired-token response can never trigger a redirect to
// /login for someone who was never logged in.
export { publicApi } from '@/lib/axios';
