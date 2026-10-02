// Runs after `vite build` (npm "postbuild"). Fills the origin placeholders in
// dist/staticwebapp.config.json (copied from public/) so the CSP names the exact API origin,
// never a wildcard, and picks report-only vs enforcing mode.
//
//   VITE_API_BASE_URL   API origin, e.g. https://crm-api-staging.azurewebsites.net (same value the
//                       bundle is built with). Required when SWA_STRICT=1.
//   SWA_BLOB_ORIGIN     Optional storage origin for img-src, e.g. https://acct.blob.core.windows.net
//   SWA_CSP_MODE        "report-only" (default) or "enforce". Exercise every screen on staging in
//                       report-only first, then switch to enforce.
//   SWA_STRICT=1        CI/CD: fail the build instead of falling back when the API origin is missing.
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const file = resolve('dist', 'staticwebapp.config.json');
const strict = process.env.SWA_STRICT === '1';

function originOf(value, name) {
  if (!value) return '';
  try {
    const url = new URL(value);
    if (url.protocol !== 'https:' && url.protocol !== 'http:') throw new Error('not http(s)');
    return url.origin;
  } catch {
    throw new Error(`${name} is not a valid http(s) URL: ${value}`);
  }
}

const apiOrigin = originOf(process.env.VITE_API_BASE_URL, 'VITE_API_BASE_URL');
const blobOrigin = originOf(process.env.SWA_BLOB_ORIGIN, 'SWA_BLOB_ORIGIN');
const mode = process.env.SWA_CSP_MODE ?? 'report-only';

if (mode !== 'report-only' && mode !== 'enforce') {
  throw new Error(`SWA_CSP_MODE must be "report-only" or "enforce", got "${mode}"`);
}
if (!apiOrigin) {
  const message = 'VITE_API_BASE_URL is not set: the CSP connect-src will allow only the site itself.';
  if (strict) throw new Error(message);
  console.warn(`[render-swa-config] ${message}`);
}

const config = JSON.parse(readFileSync(file, 'utf8'));
const headers = config.globalHeaders;

const reportOnlyKey = 'Content-Security-Policy-Report-Only';
const csp = headers[reportOnlyKey]
  .replace('__API_ORIGIN__', apiOrigin)
  .replace('__BLOB_ORIGIN__', blobOrigin)
  .replace(/\s+;/g, ';')
  .replace(/\s{2,}/g, ' ')
  .trim();

delete headers[reportOnlyKey];
headers[mode === 'enforce' ? 'Content-Security-Policy' : reportOnlyKey] = csp;

writeFileSync(file, `${JSON.stringify(config, null, 2)}\n`);
console.log(`[render-swa-config] CSP ${mode}; connect-src ${apiOrigin || "'self' only"}`);
