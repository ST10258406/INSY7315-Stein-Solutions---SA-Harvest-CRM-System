# Azure Deployment — SA Harvest CRM

How the CRM runs in Azure, which settings it needs, how a release gets there and how to roll
one back. There is **one environment: production**. There is no staging (see
[risk-register.md](risk-register.md)).

This document holds **names only**. Secret values live in Key Vault `kv-crm-k7x2` and never
appear in the repo, in workflow files or in App Service configuration as plain text.

---

## 1. Resource inventory

Resource group `rg-crm-prod`, region **South Africa North**.

| Name | Type | Purpose |
|---|---|---|
| `rg-crm-prod` | Resource group | Holds every production resource below |
| `app-crm-api-cfhsdefdhdegenc3` | App Service (Linux, B1), Web App for Containers, sidecar mode | Runs the API container. The main container is named `main`. Hostname `https://app-crm-api-cfhsdefdhdegenc3.southafricanorth-01.azurewebsites.net` |
| App Service plan of the Web App | App Service plan (B1) | Compute for the Web App. B1 has no deployment slots |
| `acrcrmk7x2` | Azure Container Registry | Stores the API images that the Web App pulls |
| `psql-crm-k7x2` | Azure Database for PostgreSQL Flexible Server, **version 18** | Application database `crmdb`. It also holds the Hangfire schema `hangfire` |
| `stcrmk7x2` | Storage account | Donor documents, in the private blob container `donor-documents` |
| `kv-crm-k7x2` | Key Vault (RBAC) | Holds the secrets. They reach the app as Key Vault references in App Service settings |
| `swa-crm-k7x2` | Static Web App (Free) | Hosts the React/Vite SPA at `https://lemon-smoke-0ec399c03.3.azurestaticapps.net` |

---

## 2. App Service settings

App Service settings arrive in the container as environment variables. A double underscore
`__` maps to `:` in .NET configuration. Some names are **flat** (for example `JWT_SECRET`)
because the code reads them by that exact name. Don't rename them to section form.

Key Vault secret names can contain only letters, digits and dashes, so the vault name differs
from the setting name. The setting's value is a reference of the form
`@Microsoft.KeyVault(SecretUri=https://kv-crm-k7x2.vault.azure.net/secrets/<secret-name>/)`.
The **Secret name in vault** column lists the names in use. If a name differs, the vault is
the source of truth (unverified from the repo).

### Secret (Key Vault references)

| App Service setting | Secret name in vault | Read by | Notes |
|---|---|---|---|
| `ConnectionStrings__Default` | *(as created in `kv-crm-k7x2`)* | EF Core, Hangfire, `/health` | Npgsql format: `Host=psql-crm-k7x2.postgres.database.azure.com;Port=5432;Database=crmdb;Username=<user>;Password=<secret>;SslMode=Require` |
| `JWT_SECRET` | *(as created)* | JWT signing and validation | At least 32 bytes, or startup fails |
| `BREVO_API_KEY` | *(as created)* | `EmailService` | Startup fails if absent. An invalid key only fails at the first send (logged to `email_logs`) |
| `ADMIN_DEFAULT_PASSWORD` | *(as created)* | `AdminUserSeeder` / `AdminSeedGuard` | Required on the first boot (empty `users` table). At least 12 characters, with an uppercase letter, a digit and a symbol, and not a known default |
| `Azure__BlobStorage__ConnectionString` | *(as created)* | `BlobStorageService` | Must contain the account key, because SAS URLs are signed with it |

### Non-secret (plain App Service settings)

| App Service setting | Value (shape) | Notes |
|---|---|---|
| `WEBSITES_PORT` | `8080` | The container listens on 8080 as a non-root user |
| `ASPNETCORE_ENVIRONMENT` | `Production` (or unset) | **Never** `Development` (opens the Hangfire dashboard, enables Swagger, seeds fake data) or `Testing` (skips migrations) |
| `Cors__AllowedOrigins__0` | `https://lemon-smoke-0ec399c03.3.azurestaticapps.net` | An exact https origin. No path, no wildcard |
| `Frontend__BaseUrl` | `https://lemon-smoke-0ec399c03.3.azurestaticapps.net` | Used to build password-reset and public-form links |
| `ADMIN_EMAIL` | a real, reachable address | Checked on every boot. `.local`, `.test`, `.example`, `example.com` and similar are rejected |
| `Auth__RefreshCookie__SameSite` | `None` | Required because the SPA and API are on different sites (`*.azurestaticapps.net` and `*.azurewebsites.net`) |
| `Brevo__SenderEmail` | a sender verified in Brevo | Overrides the default in `appsettings.json` |
| `Azure__BlobStorage__ContainerName` | `donor-documents` | Same as the code default |

Leave these **unset** in Azure: `Azure__BlobStorage__PublicEndpoint` (only for Azurite),
`ASPNETCORE_HTTPS_PORT` and `HTTPS_PORT` (with these set, health probes would be redirected),
`Auth__RefreshCookie__Secure` (it defaults to `true`, and `false` blocks startup), and
`AZURE_KEY_VAULT_URI` (no code reads it).

### Optional (code defaults apply)

`Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenExpiryMinutes` (60),
`Auth__RefreshToken__LifetimeDays` (7), `Auth__RefreshToken__ReuseGraceSeconds` (20),
`Auth__Lockout__MaxFailedAttempts` (5), `Auth__Lockout__LockoutMinutes` (15),
`RateLimiting__DonorEmail__*`, `RateLimiting__Auth__{Login|ResetPassword|Refresh}__*`,
`Brevo__SenderName`, `Brevo__OrganisationName`, `Brevo__FooterDetails`, `Brevo__LogoUrl`.

### Platform settings (not app settings)

- **Always On**: on. Without it the in-process Hangfire server sleeps and the 04:00 UTC
  task-due job is missed.
- **HTTPS Only**: on. TLS ends at the App Service front end, and the container only sees HTTP.
- **Health check path**: `/health`.
- **Identity**: system-assigned managed identity on. It needs *Key Vault Secrets User* on
  `kv-crm-k7x2` and *AcrPull* on `acrcrmk7x2`.

---

## 3. Deploy flow

```
feature/*  ──PR──▶  Development  ──PR──▶  main
              CI only               CI + deploy to production
```

- `backend.yml` and `frontend.yml` (CI) run on every push and PR to `Development` and `main`.
  They build and test only and never deploy.
- **A merge to `main` deploys to production.** There is no staging step in between.
  - **API:** `backend-deploy.yml` builds the image with `src/CRM.API/Dockerfile` (the build
    context is the repo root), pushes it to `acrcrmk7x2`, and updates the Web App's `main`
    container to the new tag.
  - **Frontend:** `frontend-deploy.yml` runs on pushes to `main` that touch `src/CRM.Web/**`,
    or on manual dispatch. It builds the SPA in the job on Node 26. The production API origin
    and blob origin are inlined at build time and written into the CSP in
    `dist/staticwebapp.config.json`. It then uploads `src/CRM.Web/dist` with
    `Azure/static-web-apps-deploy` (`skip_app_build: true`), using the repository secret
    `AZURE_STATIC_WEB_APPS_API_TOKEN`.
- Both deploy jobs use the GitHub `production` environment, so a required reviewer has to
  approve each run.
- On container start the API applies EF Core migrations, runs the idempotent seeders, creates
  or updates the Hangfire schema, and registers the recurring job. Then it starts listening.

> The repo did not contain `backend-deploy.yml` when this document was written. The
> description above is the intended behaviour. Check it against the workflow once it is
> committed.

---

## 4. Rolling back

### API

Images are immutable and tagged per build. To roll back, re-run `backend-deploy.yml` with a
**previous image tag** (manual dispatch, if the workflow takes a tag input), so the Web App's
`main` container points at the older image again.

**Database migrations do not roll back with the image.** The app only migrates forwards on
startup. If the release you're reverting added a migration, the older image runs against the
newer schema. That is safe for additive migrations (new columns or tables). Before reverting
past a destructive migration, restore the database from a point-in-time backup to a new
server first, and decide deliberately.

### Frontend

Re-run `frontend-deploy.yml` from the previous good commit on `main` (or revert the commit on
`main`). Static Web Apps (Free) has no one-click version history.

---

## 5. One-time portal step: point `main` at ACR with managed identity

This has to be done once, by hand, before the first automated deploy:

1. Web App `app-crm-api-cfhsdefdhdegenc3` → **Identity** → System assigned → **On**.
2. Container registry `acrcrmk7x2` → **Access control (IAM)** → add role assignment
   **AcrPull** → the Web App's managed identity.
3. Web App → **Deployment Center** (sidecar containers) → select the `main` container:
   - Registry source: Azure Container Registry, registry `acrcrmk7x2`
   - Authentication: **Managed identity** (system assigned). Not admin credentials.
   - Image: the API repository, with an existing tag
   - Port: `8080`
4. Save, then watch the log stream until `/health` returns 200.

Role assignments can take several minutes to propagate. An image-pull 401 right after step 2
is usually just timing.

---

## 6. First-deploy troubleshooting

Start with **Log stream** (or `az webapp log tail`). The API logs to the console through
Serilog.

### The container exits straight away: a startup guard

Outside Development the API refuses to start when it detects one of these. Each message
names the setting and never prints a value.

| Message contains | Fix |
|---|---|
| `Invalid configuration for the 'Production' environment` followed by a list | Fix every listed item. Typical causes: `Cors:AllowedOrigins` is empty, uses http, has a path, a `*` or a localhost value; `Frontend:BaseUrl` is empty, http or localhost; `JWT_SECRET` is shorter than 32 bytes; `Auth:RefreshCookie:Secure` is false |
| `JWT_SECRET is not set` / `BREVO_API_KEY is not set` | The setting is missing, or its Key Vault reference didn't resolve (see below) |
| `ADMIN_EMAIL is not set` / `is not a valid, routable email address` | Set a real address. Reserved test domains are rejected |
| `ADMIN_DEFAULT_PASSWORD is not acceptable outside Development` | Generate a stronger value (at least 12 characters, with an uppercase letter, a digit and a symbol) |
| `ADMIN_DEFAULT_PASSWORD is not set` | The `users` table is empty on the first boot, so a password is needed |

### Key Vault references

- In the portal, open **Environment variables**. Each reference shows a status, and a green
  tick means it resolved.
- A reference that fails to resolve can reach the app as the literal `@Microsoft.KeyVault(...)`
  string. `JWT_SECRET` would still pass the length check, and any token signed with it is
  useless. Check every status before trusting a green `/health`.
- Common causes: the managed identity is missing *Key Vault Secrets User*; a typo in the
  secret name or URI; the role assignment hasn't propagated yet.
- After fixing a reference, restart the app. References refresh periodically, and a restart
  forces it.

### Database connection

- Timeout or `No such host`: check the server name in `ConnectionStrings__Default` and the
  Postgres firewall. It must allow the App Service outbound IPs (or "Allow public access from
  Azure services").
- `password authentication failed`: the Flexible Server username has **no** `@server` suffix.
- SSL errors: the server enforces TLS. Use `SslMode=Require` (or `VerifyFull`; see the risk
  register).
- `permission denied for schema` / `CREATE`: the app user needs DDL rights. On startup it runs
  migrations, creates sequences and creates the Hangfire schema.
- A log line `libgssapi_krb5.so.2: cannot open shared object file` is harmless. Npgsql probes
  for Kerberos and falls back to password auth.
- On a fresh database, one `ERR Failed executing DbCommand ... __EFMigrationsHistory` line is
  expected: EF probes for the history table before creating it.

### `/health`

- `GET /health` is anonymous and runs a database check. It returns `Healthy` (200) or
  `Unhealthy` (503).
- A 503 means the app is up but can't reach Postgres. See the section above.
- No response at all means the container isn't listening. Check `WEBSITES_PORT=8080`.
- A 307 means an HTTPS port was configured. Remove `ASPNETCORE_HTTPS_PORT`/`HTTPS_PORT`.
- A one-off warning `Failed to determine the https port for redirect` is expected and harmless.

### The SPA loads but nothing works

- The browser console shows CORS errors: `Cors__AllowedOrigins__0` must match the SWA URL
  exactly.
- Users are signed out on every reload: check `Auth__RefreshCookie__SameSite=None`. Safari
  (and any browser blocking third-party cookies) will still sign users out. A shared custom
  domain is the fix.
- Calls go to the SWA origin instead of the API: the frontend was built without
  `VITE_API_BASE_URL`. The deploy workflow sets it, and `SWA_STRICT=1` fails such a build.
