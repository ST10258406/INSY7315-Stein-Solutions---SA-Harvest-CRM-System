# SA Harvest CRM

A purpose-built donor-management CRM for **SA Harvest**, a South African non-profit that
recovers and redistributes food to beneficiaries. It replaces the organisation's
Monday.com donor board with a self-hosted system that removes the recurring SaaS cost
while replicating (and improving on) the existing donor workflow.

Built as an INSY7315 final-year Work Integrated Learning project by **Stein Solutions**.

> Scope is the **donor-management board only**. Beneficiary/CBO vetting, the Foodspace
> ERP integration, a donor portal, and a mobile app are explicitly out of scope for the
> initial build (see the System Design Document, §1.5).

---

## Table of contents

- [What it does](#what-it-does)
- [Tech stack](#tech-stack)
- [Architecture at a glance](#architecture-at-a-glance)
- [Repository layout](#repository-layout)
- [Getting started](#getting-started)
  - [Option A — Docker Compose (recommended)](#option-a--docker-compose-recommended)
  - [Option B — run each part on the host](#option-b--run-each-part-on-the-host)
- [Environment variables](#environment-variables)
- [Common commands](#common-commands)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Deployment (Azure)](#deployment-azure)
- [Documentation](#documentation)
- [Contributing](#contributing)

---

## What it does

| Capability | Summary |
|---|---|
| **Donor profiles** | Full company, contact, address, donation and compliance data per donor. Lightweight DTO on list views, full detail only on record open. |
| **Dual entry** | Public onboarding form (no login, canvas signature, rate-limited) **and** manual internal capture. Both land as `PendingReview` and carry a `SubmissionSource`. |
| **Approval workflow** | Every new donor is reviewed by an Admin. Approve → `Active`; reject → `Rejected` with a required reason. Every decision writes a `DonorApproval` record. |
| **Interaction logging** | Append-only interaction timeline per donor — the evidence base for the "Donors Contacted" monthly KPI. Logs can never be edited or deleted. |
| **Follow-ups & tasks** | Per-donor follow-up date, daily reminder jobs, assignable tasks with due-date notifications. Overdue items surface on the dashboard. |
| **Documents** | BBBEE certificates and signatures in Azure Blob Storage. Access is Admin-only for certificates and always via short-lived (15-minute) SAS URLs — raw blob URLs are never exposed. |
| **Email** | Outbound email via SendGrid, auto-logged as an `Email` interaction; forwarded emails attach to the donor card. |
| **Reports & dashboards** | Donors contacted, by region, by donation type, by status. Exports rendered server-side to PDF/Excel and returned as SAS URLs. |
| **Users & roles** | Admin-only user management for 10–20 internal staff. Four roles: `Marketing` < `Procurement` < `Admin` < `SuperAdmin`. |
| **Auditability** | Append-only `audit_logs` for every mutating action, written automatically by a MediatR pipeline behaviour. |

Donors never log in. The primary users are the NPO's procurement and marketing teams.

---

## Tech stack

**Backend**

- ASP.NET Core 10 Web API on **.NET 10**, C# 13
- **Modular Monolith** + **Clean Architecture** (Domain → Application → Infrastructure → API)
- **CQRS** via MediatR, with three pipeline behaviours: `LoggingBehaviour` → `ValidationBehaviour` → `AuditBehaviour`
- **PostgreSQL 16** via EF Core 10 + Npgsql (code-first migrations, Fluent API only)
- **FluentValidation** for all command/query validation
- **AutoMapper** for entity → DTO projection
- **Hangfire** (+ `Hangfire.PostgreSql`) for scheduled/background jobs
- **JWT Bearer** auth with a standalone `PasswordHasher<User>` — **no `IdentityDbContext`, no `UserManager<T>`** (see [Developer Guide](docs/DEVELOPER_GUIDE.md#authentication))
- **Serilog** for structured logging
- **Azure Blob Storage** (Azurite locally) for documents; **Azure Key Vault** for secrets in production
- **Swagger / Swashbuckle** for API docs (Development only)

**Frontend** (`src/CRM.Web`)

- **React 19** + **TypeScript** + **Vite**
- **TanStack Query** for all server state; **Zustand** for client state (auth, UI) — **no Redux**
- **React Hook Form** + **Zod** for forms and validation
- **shadcn/ui** + **Tailwind CSS v4**
- **React Router v7**
- **Axios** via a single configured instance (`src/lib/axios.ts`) with JWT + 401-refresh interceptors
- **Vitest** + Testing Library

---

## Architecture at a glance

```
┌─────────────────────────────────────────────────────────┐
│  CRM.API            HTTP, Controllers, Middleware, DI    │  ← outermost
│  CRM.Infrastructure EF Core, Azure, SendGrid, Hangfire   │
│  CRM.Application     CQRS handlers, validators, interfaces│
│  CRM.Domain          Entities, enums, rules — 0 deps     │  ← innermost
└─────────────────────────────────────────────────────────┘
Dependencies point inward only.
```

- **`CRM.Domain`** — entities and enums, **zero NuGet references**.
- **`CRM.Application`** — business logic as MediatR commands/queries + handlers + validators, organised by module under `Modules/`. Defines interfaces (`IDonorRepository`, `IBlobStorageService`, `ICurrentUserService`, …). **No EF Core, no Azure, no ASP.NET.**
- **`CRM.Infrastructure`** — the only place EF Core queries live (repositories in `Persistence/Repositories/`), plus Azure/SendGrid/Hangfire implementations and all migrations.
- **`CRM.API`** — thin controllers that map an HTTP request to a command/query and return the result. Role checks live here via `[Authorize(Policy = "…")]`.

The full rationale, layer rules, and non-negotiables are in the
[Developer Guide](docs/DEVELOPER_GUIDE.md) and `.github/copilot-instructions.md`.

---

## Repository layout

```
.
├── CRM.slnx                        # .NET solution (slnx format)
├── docker-compose.yml              # postgres + azurite + crm-api + crm-web
├── docker-compose.override.yml     # local dev overrides
├── .env.example                    # copy to .env
├── .github/
│   ├── copilot-instructions.md     # PR review checklist / coding standards (authoritative)
│   └── workflows/                  # backend.yml, frontend.yml
├── docs/
│   └── DEVELOPER_GUIDE.md          # standards new contributors must follow
├── src/
│   ├── CRM.Domain/                 # Entities/, Enums/, Common/ (BaseEntity, LookupBaseEntity)
│   ├── CRM.Application/
│   │   ├── Common/                 # Behaviours/, Interfaces/, Exceptions/, Models/
│   │   └── Modules/                # Auth/, Donors/, Lookups/ … (Commands/ Queries/ Dtos/ Mappings/)
│   ├── CRM.Infrastructure/
│   │   ├── Persistence/            # CrmDbContext, Configurations/, Repositories/, Interceptors/, Seeders/, Migrations/
│   │   ├── Auth/                   # JwtTokenService, JwtSettings
│   │   └── Services/               # BlobStorageService, EmailService, NotificationService, CurrentUserService
│   └── CRM.Web/                    # React frontend (see its own structure below)
└── tests/
    ├── CRM.Application.Tests/      # handler + validator unit tests (xUnit)
    ├── CRM.Infrastructure.Tests/  # repository / interceptor / EF config tests
    └── CRM.API.Tests/             # controller + middleware + authorization tests
```

Frontend (`src/CRM.Web/src`):

```
features/<feature>/     # feature-first vertical slices: components/ hooks/ pages/ schemas/
components/ui/          # shadcn/ui generated — never edited by hand
components/pages/        # route-level page shells
lib/axios.ts            # the ONLY axios instance feature code may import
lib/apiError.ts         # error-envelope parsing
routes/paths.ts         # every route string as a typed constant — never hardcode routes
store/authStore.ts      # Zustand auth state (in-memory only, no localStorage)
services/               # thin API service wrappers
```

> The frontend project folder is `CRM.Web` (PascalCase) — it was renamed from `crm-web`.
> Watch for stale lowercase references in scripts, Compose, or CI cache paths.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 26](https://nodejs.org/) (frontend)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the Compose stack, or just Postgres + Azurite)
- An IDE: Visual Studio 2022+, Rider, or VS Code with the C# Dev Kit

### First-time setup

```bash
git clone <repo-url>
cd INSY7315-Stein-Solutions---SA-Harvest-CRM-System
cp .env.example .env
# edit .env — set DB_PASSWORD, JWT_SECRET (≥ 256 bits), ADMIN_DEFAULT_PASSWORD
```

### Option A — Docker Compose (recommended)

Brings up PostgreSQL, Azurite (blob emulator), the API, and the Vite dev server:

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger (Development only) |
| Health check | http://localhost:5000/health |
| Hangfire dashboard | http://localhost:5000/hangfire |
| Frontend | http://localhost:3000 |
| PostgreSQL | `localhost:5432` (db `crmdb`) |
| Azurite (blob) | `localhost:10000` |

The API applies EF Core migrations and runs the database seeders
(`RoleSeeder`, `LookupSeeder`, `AdminUserSeeder`) automatically on startup in every
environment except `Testing`.

### Seed data & default login (local development)

On first run against an empty database the seeders create:

- **4 roles** — `SuperAdmin`, `Admin`, `Procurement`, `Marketing` (`RoleSeeder`).
- **All lookup / reference data** — company types, entity types, provinces, operational
  regions, donation types, donation frequencies, BBBEE statuses (`LookupSeeder`).
- **One user** — a single bootstrap SuperAdmin (`AdminUserSeeder`):

  | Field | Value |
  |---|---|
  | Email | `admin@crm.local` |
  | Name | System Administrator |
  | Role | `SuperAdmin` |
  | Password | whatever `ADMIN_DEFAULT_PASSWORD` is set to in your `.env` — the committed `.env.example` uses **`ChangeMe123!`** |
  | Active | yes |

  The password is **never** stored in code or in a migration — `AdminUserSeeder` reads
  `ADMIN_DEFAULT_PASSWORD` from configuration (`.env` locally, Key Vault in production),
  hashes it with `PasswordHasher<User>`, and the seeder throws on startup if the value is
  missing. No other users are seeded — create the rest via the user-management API/UI once
  logged in as the SuperAdmin.

Each seeder is idempotent (guards on "any rows already exist"), so restarting the API
won't duplicate data. To start completely fresh, drop the volume:
`docker compose down -v` (this also wipes Azurite blobs), then bring the stack back up.

> These credentials are for **local development only**. In production `ADMIN_DEFAULT_PASSWORD`
> comes from Key Vault and must be rotated immediately after the first sign-in.

### Option B — run each part on the host

Start only the infrastructure containers:

```bash
docker compose up postgres azurite
```

Then the API (from the repo root):

```bash
dotnet restore CRM.slnx
dotnet run --project src/CRM.API
# http://localhost:5278  (https://localhost:7062)
```

And the frontend:

```bash
cd src/CRM.Web
npm install
npm run dev
# http://localhost:3000  — Vite proxies /api to http://localhost:5278
```

---

## Environment variables

Copy `.env.example` to `.env`. Nothing secret is ever committed — locally it lives in
`.env` (git-ignored) or .NET user-secrets; in production it comes from **Azure Key Vault**
via Managed Identity.

| Variable | Used by | Notes |
|---|---|---|
| `DB_USER`, `DB_PASSWORD` | Postgres container | Local only |
| `DATABASE_URL` | API | Npgsql connection string (`ConnectionStrings__Default`) |
| `JWT_SECRET` | API | Signing key, **≥ 256 bits**. Never hardcoded/committed |
| `SENDGRID_API_KEY` | API | Outbound email |
| `AZURE_STORAGE_CONNECTION_STRING` | API | Points at Azurite locally; real storage in prod |
| `AZURE_KEY_VAULT_URI` | API | Secret source in production |
| `ADMIN_DEFAULT_PASSWORD` | API seeder | Initial SuperAdmin password — change after first login |
| `VITE_API_BASE_URL` | Frontend | e.g. `http://localhost:5000` |
| `VITE_USE_POLLING` | Frontend (Vite) | Set `true` only for Docker-on-Windows file watching |

JWT config that must not drift: **HS256**, 60-minute access token, 7-day refresh token
stored in the DB and revoked on logout.

---

## Common commands

**Backend** (repo root)

```bash
dotnet build CRM.slnx                        # build everything
dotnet test CRM.slnx                         # run all test projects
dotnet run --project src/CRM.API             # run the API

# EF Core migrations (from repo root)
dotnet ef migrations add <Name> --project src/CRM.Infrastructure --startup-project src/CRM.API
dotnet ef database update   --project src/CRM.Infrastructure --startup-project src/CRM.API
```

**Frontend** (`src/CRM.Web`)

```bash
npm run dev        # Vite dev server on :3000
npm run build      # tsc -b && vite build
npm run lint       # ESLint
npx tsc --noEmit   # type-check only (matches CI)
npx vitest         # unit tests
```

---

## Testing

| Project | What it covers | Notes |
|---|---|---|
| `CRM.Application.Tests` | Command/query **handlers** and **validators** in isolation | Pure unit tests, no DB |
| `CRM.Infrastructure.Tests` | Repositories, `UpdatedAtInterceptor`, EF configurations, `JwtTokenService` | Uses `TestPostgres` |
| `CRM.API.Tests` | Controllers, `ExceptionHandlingMiddleware`, authorization handlers | |
| `CRM.Web` (`*.test.tsx`) | Auth store, route guards, auth pages | Vitest + Testing Library |

Every new handler or validator **must** ship with unit tests. Run `dotnet test CRM.slnx`
and `npx tsc --noEmit` before opening a PR.

---

## CI/CD

GitHub Actions run on push and PR to `main` and `Development`:

- **`backend.yml`** — spins up a Postgres 16 service, then `dotnet restore` / `build --configuration Release` / `test` against `CRM.slnx` on .NET 10.
- **`frontend.yml`** — `npm ci`, `npx tsc --noEmit`, `npm run build` in `src/CRM.Web` on Node 26.

A PR is mergeable only when both pipelines pass, the solution builds with **zero warnings**,
TypeScript compiles with zero errors, and no item in the
[Non-Negotiables](docs/DEVELOPER_GUIDE.md#-non-negotiables) list is violated.

---

## Deployment (Azure)

| Component | Azure service |
|---|---|
| API | Azure App Service (Docker container, multi-stage build → Azure Container Registry) |
| Frontend | Azure Static Web Apps (Vite build output, served from CDN — not containerised) |
| Database | Azure Database for PostgreSQL — Flexible Server |
| File storage | Azure Blob Storage (BBBEE certs, signatures, report exports, forwarded email) |
| Secrets | Azure Key Vault, read at runtime via Managed Identity |

No secret ever appears in `appsettings.json`, a Docker image, or a committed `.env`.

---

## Documentation

- **[docs/DEVELOPER_GUIDE.md](docs/DEVELOPER_GUIDE.md)** — the standards every contributor must follow.
- **`.github/copilot-instructions.md`** — the PR review checklist. On any conflict about
  **auth architecture**, this file is authoritative over the System Design Document §5.7.
- **`Documentation/SA Harvest CRM — System Design Document.md`** — requirements, data model
  (21 tables / 7 domains), API design, and the architectural decision log.

---

## Contributing

Read the [Developer Guide](docs/DEVELOPER_GUIDE.md) first. In short:

1. Branch off `Development`.
2. Keep the Clean Architecture layer boundaries intact — the dependency rule is enforced in review.
3. Add a command/query + handler + validator for new behaviour; don't put logic in controllers.
4. Write unit tests for every new handler and validator.
5. `dotnet build` (zero warnings), `dotnet test`, `npx tsc --noEmit` all green.
6. Open the PR against `Development`; CI must pass and the Non-Negotiables checklist must hold.
