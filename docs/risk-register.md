# Risk Register — SA Harvest CRM (first production deploy)

Risks consciously accepted for the first production deployment, each with an owner and a
trigger for revisiting it. Finding IDs (F-xx) refer to [security-review.md](security-review.md).
Severity uses the review's scale (High / Medium / Low).

Owner for every row: **Coach**.

## Accepted risks

| # | Risk | Severity | Rationale for accepting | Revisit when |
|---|---|---|---|---|
| R-01 | The storage account key sits in `Azure__BlobStorage__ConnectionString`, and shared-key access is still enabled on `stcrmk7x2` (F-12). Anyone holding the key has full access to every donor document | Medium | The app has no managed-identity code path yet: `BlobStorageService` signs SAS URLs with the account key. The key is stored in Key Vault, not in plain configuration | Sprint 7 #13 (user-delegation SAS) is picked up; the key may have leaked; or before real donor documents are uploaded at scale. Rotate the key after any exposure |
| R-02 | Key Vault, the storage account and Postgres are reachable from the internet (authenticated, not network-restricted) | Medium | B1 has no VNet integration budget in this project. Every service still needs a credential or identity | Before go-live with real donor data, or if the budget allows VNet integration and private endpoints |
| R-03 | The app connects to Postgres as the **server admin** (F-14) | Medium | The app runs migrations, creates sequences and creates the Hangfire schema at startup, so it needs DDL rights. A least-privilege role needs a separate migration step first | Sprint 7 #14 ships (migrations moved out of startup), or the first time a second app or person needs database access |
| R-04 | Migrations and seeding run at app startup, on a single instance, with no off switch (F-14) | Medium | Simple and proven in the local Production rehearsal. A single B1 instance avoids concurrent migrators | Scaling out to more than one instance, adding a destructive migration, or Sprint 7 #14 |
| R-05 | The deploy identity (user-assigned managed identity `id-gh-crm-deploy`) has **Contributor** on the whole `rg-crm-prod` resource group | Medium | It was quickest to get CD working. The GitHub `production` environment's required reviewer gates every run | Once the deploy is stable: scope it down to AcrPush on `acrcrmk7x2` plus Website Contributor on the Web App |
| R-06 | No staging environment. Changes go from `Development` (CI only) straight to production | Medium | University project budget and timeline. The Production-mode container rehearsal and CI on Postgres 18 partly make up for it | Before SA Harvest relies on it operationally, or after any production incident that staging would have caught |
| R-07 | ACR images are not vulnerability-scanned | Low | Defender for Containers has a cost. The base image is the official `aspnet:10.0`, rebuilt on every deploy | A base-image CVE is announced, or the budget allows Defender for Containers |
| R-08 | Uploaded documents are validated by file signature (magic bytes), but they are **not malware-scanned**: Defender for Storage is not enabled on `stcrmk7x2`. Downloads are forced to `Content-Disposition: attachment` (Sprint 7 #6) | Medium | Defender for Storage has a per-transaction cost. Signature validation stops disguised file types (e.g. an executable renamed `.pdf`), but a malicious *real* PDF or image still gets through. Anonymous donors can upload files that Admins later open | Before the public donor form is advertised widely, a suspicious upload, or when the budget allows enabling Defender for Storage malware scanning on `donor-documents` |
| R-09 | The refresh cookie is `SameSite=None` across `*.azurestaticapps.net` and `*.azurewebsites.net`. Safari, and browsers blocking third-party cookies, drop it, so those users are signed out on every reload | Low | Using the default Azure hostnames is the only option without a custom domain. Chrome and Edge work. The CSRF header check (`X-Requested-With`) still protects refresh and logout | A custom domain is bought: put the SPA and API under one registrable domain (e.g. `crm.` and `api.`) and switch back to `SameSite=Strict` |
| R-10 | `SslMode=Require` in the Postgres connection string encrypts traffic but does not verify the server certificate (Npgsql 8+ semantics) | Low | Traffic stays inside Azure's network to a known hostname. `VerifyFull` depends on the container's CA bundle, which hasn't been verified against Azure yet | Once `SslMode=VerifyFull` has been tested against `psql-crm-k7x2` from the container |
| R-11 | Single instance, no high availability (B1, one Postgres server without HA) | Low | Cost. Acceptable downtime for an NGO back-office tool | SA Harvest needs an uptime commitment, or B1 restarts become frequent |
| R-12 | The Hangfire dashboard is effectively unreachable in Production. It needs an authenticated `Admin`, but auth is bearer-only, so browser navigation is never authenticated (F-36) | Low | Safe by default. Job status can be read from the `hangfire` schema, and failures also show in `email_logs` | Someone needs to operate jobs interactively |
| R-13 | The Brevo sender is not yet a verified domain (F-24). Password-reset and invite emails may land in spam or be rejected | Medium | Sprint 7 #23 hasn't shipped. Delivery is logged in `email_logs`, so failures are visible | Before inviting real staff or donors: verify a sender domain in Brevo (SPF/DKIM/DMARC) and set `Brevo__SenderEmail` |

## Sprint 7 issues not shipped

Status is judged from the code on `Development` at `bbe1cdf` and from
`../Documentation/sprint-7-issues.md`. GitHub issue states could not be read when this was
written, so check them against the board.

| Issue | Title | Tier | Status in code | Related risk |
|---|---|---|---|---|
| #8 | Bot Protection (Turnstile) + Global Submission Ceiling | 3 | Not shipped. No CAPTCHA, and the global limiter is still per IP | Public-form spam and harassment through SA Harvest's sender reputation (F-09) |
| #9 | Auth-Aware Global Rate Limiter | 2 | Not shipped. The global limiter is still 100/min **per IP**, hardcoded | Staff behind one office NAT can hit `429` (F-10) |
| #10 | Forgot-Password Timing Fix | 2 | Not shipped. The handler still awaits the Brevo send in-request | Account enumeration by timing (F-17) |
| #12 | Azure Provisioning (Staging + Production) | 1 | Partly done. Production only (no staging, R-06). Defender for Storage not enabled (R-08). ACR scanning off (R-07) | R-02, R-06, R-07, R-08 |
| #13 | Key Vault, Managed Identity and User-Delegation SAS | 1 | Partly done. Key Vault **references** are used, but there is no managed-identity blob access and shared-key access is still enabled | R-01 |
| #14 | Migration Strategy and Least-Privilege Database Role | 1 | Not shipped | R-03, R-04 |
| #16 | Audit Log Gaps (POPIA) | 2 | Not shipped. `AuditBehaviour` still writes `IpAddress`/`UserAgent` as null, and document upload/delete commands aren't auditable | Incomplete POPIA audit trail (F-19) |
| #18 | Frontend Dependency Remediation + Dead Code Cleanup | 1 | Partly done. Dead `loginApi` still in `authService.ts`. Dependency advisories not re-checked | F-18, F-35 |
| #19 | CI Security Gates + CD Workflows (OIDC) | 1 | Partly done. `backend-deploy.yml` (OIDC) and `frontend-deploy.yml` added. The CI workflows have no `permissions:` block and no security scanning, and the deploy does not wait for CI on the merge commit | F-27 |
| #20 | Unit Test Gap-Fill | 2 | Unverified | — |
| #21 | Integration Tests on Real PostgreSQL + Security Regression Suite | 1 | Partly done. Repository tests run on real Postgres (passing on 18); API tests use the in-memory provider and in-memory Hangfire | F-28 |
| #22 | Backend Hygiene Cleanup | 3 | Unverified | — |
| #23 | Brevo Sender Domain + Email Deliverability | 2 | Not shipped | R-13 |
| #24 | Staging Verification of Unverified Review Items | 1 | Not possible as written (no staging). Must be done carefully against production instead | R-06 |
| #25 | End-to-End Staging Smoke Test and UAT Checklist | 1 | Not in the repo | R-06 |
| #26 | Handover Documentation, Runbook and Risk Register | 1 | In progress: [azure-deployment.md](azure-deployment.md) and this register | — |
