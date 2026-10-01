# Developer Guide — SA Harvest CRM

This document is the practical companion to `.github/copilot-instructions.md`. It tells a
new contributor **how the codebase is organised, what standards it must adhere to, and how
to add a feature without breaking the architecture.**

Where this guide and `.github/copilot-instructions.md` overlap, they are meant to agree.
If they ever disagree, **`.github/copilot-instructions.md` wins** — it is the checklist
reviewers (human and Copilot) run against every PR.

---

## Contents

1. [Ground rules](#1-ground-rules)
2. [Architecture & the dependency rule](#2-architecture--the-dependency-rule)
3. [Backend conventions](#3-backend-conventions)
4. [CQRS: adding a command or query](#4-cqrs-adding-a-command-or-query)
5. [Persistence & EF Core](#5-persistence--ef-core)
6. [Authentication](#6-authentication)
7. [Authorization](#7-authorization)
8. [API contract](#8-api-contract)
9. [Data rules](#9-data-rules)
10. [Frontend conventions](#10-frontend-conventions)
11. [Testing standards](#11-testing-standards)
12. [Git workflow & Definition of Done](#12-git-workflow--definition-of-done)
13. [🔴 Non-Negotiables](#-non-negotiables)

---

## 1. Ground rules

- **Modular Monolith, not microservices.** Module boundaries live in
  `CRM.Application/Modules/`. They run in one process and communicate through the database
  or MediatR — never by one module calling another module's classes directly.
- **Clean Architecture, four layers.** Business logic must be testable with no ASP.NET and
  no EF Core in scope.
- **No hard deletes, ever.** Soft-delete (`is_active = false`) or status-transition.
- **Two append-only tables:** `interaction_logs` and `audit_logs`. No `UPDATE`, no
  `DELETE`, no `UpdatedAt` property, ever.
- **Secrets never touch source control.** Local: `.env` / user-secrets. Production: Key Vault.
- **Reference data lives in lookup tables**, not enums-as-dropdowns and not hardcoded lists.

---

## 2. Architecture & the dependency rule

```
CRM.API  ──────────────►  CRM.Application  ──────────►  CRM.Domain
   │                          ▲     ▲
   └──► CRM.Infrastructure ───┘     │  (implements Application interfaces)
        CRM.Infrastructure ────────►┘  CRM.Infrastructure ──► CRM.Domain
```

| Layer | May reference | Must **not** reference |
|---|---|---|
| `CRM.Domain` | `System.*` and itself only | **Any** NuGet package. Zero `<PackageReference>`. |
| `CRM.Application` | `CRM.Domain`, MediatR, FluentValidation, AutoMapper, `Microsoft.Extensions.Identity.Core` | `Microsoft.EntityFrameworkCore` (**any** namespace, including the core package), `Azure.Storage.Blobs`, `SendGrid`, `Microsoft.AspNetCore.*` |
| `CRM.Infrastructure` | `CRM.Application`, `CRM.Domain` | — |
| `CRM.API` | `CRM.Application` (dispatch), `CRM.Infrastructure` (**DI registration only**) | Business logic of its own |

Checkable invariant: `grep -rn "Microsoft.EntityFrameworkCore" src/CRM.Application/`
**must return nothing.** There is deliberately no `IApplicationDbContext` — exposing
`DbSet<T>` from Application is what forced the EF Core dependency last time. Do not
reintroduce it.

**Controllers are thin.** A controller method may only: receive the request → map it to a
Command/Query → `await _mediator.Send(...)` → return the HTTP result. If a controller has
`if`/`else`, a loop, a DB call, or a direct service call, it's wrong.

---

## 3. Backend conventions

### Naming (C#)

| Kind | Convention | Example |
|---|---|---|
| Classes / methods / properties | PascalCase | `GetDonorByIdAsync`, `CompanyName` |
| Interfaces | `IPascalCase` | `IBlobStorageService` |
| Private fields | `_camelCase` | `_mediator`, `_context` |
| Enums | PascalCase | `DonorStatus.Active` |
| Constants | `UPPER_SNAKE` | `MAX_FILE_SIZE_BYTES` |
| Async methods | `Async` suffix (and only when actually async) | `CreateDonorAsync` |
| Test methods | `Method_Scenario_ExpectedResult` | `CreateDonor_WithInvalidTaxNumber_ReturnsValidationError` |

### Entity rules

- The task entity is **`DonorTask`**, never `Task` (collides with `System.Threading.Tasks.Task`). This is permanent.
- Status/type fields are **enums**, never strings, on the domain entity:
  `Donor.Status` → `DonorStatus`, `DonorContact.ContactType` → `ContactType`,
  `InteractionLog.InteractionType` → `InteractionType`,
  `DonorDocument.DocumentType` → `DocumentType`, `DonorApproval.Status` → `ApprovalStatus`,
  `Notification.NotificationType` → `NotificationType`, `AuditLog.Action` → `AuditAction`.
- Entities carry no data annotations (`[Required]`, `[MaxLength]`, `[Column]`, …). All
  mapping is Fluent API in `IEntityTypeConfiguration<T>` under
  `CRM.Infrastructure/Persistence/Configurations/`.
- Main entities extend `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`). Append-only entities
  have `CreatedAt` only.
- Lookups extend `LookupBaseEntity` and use `SMALLINT` identity PKs.

### Logging

Use Serilog structured logging (`Log.Information("... {Field}", value)`), never string
interpolation into the message template.

---

## 4. CQRS: adding a command or query

Every unit of behaviour is a MediatR message. Folder layout inside a module:

```
Modules/<Module>/
├── Commands/<Name>/
│   ├── <Name>Command.cs          # input record/class, implements IRequest<TResult>
│   ├── <Name>CommandHandler.cs   # the business logic
│   └── <Name>CommandValidator.cs # AbstractValidator<<Name>Command>
├── Queries/<Name>/
│   ├── <Name>Query.cs
│   ├── <Name>QueryHandler.cs
│   └── <Name>QueryValidator.cs   # when the query takes parameters worth validating
├── Dtos/                         # <Entity>Dto.cs (responses), <Entity>Request.cs (inputs)
└── Mappings/<Module>MappingProfile.cs   # AutoMapper profile
```

MediatR, validators, and AutoMapper profiles are auto-registered by assembly scan in
`AddApplicationServices()` — you don't wire them up manually.

### Pipeline behaviours (run around every `Send`)

Registered order in `ServiceCollectionExtensions.AddApplicationServices`:
`LoggingBehaviour` → `ValidationBehaviour` → `AuditBehaviour`.

| Behaviour | Does | Your handler therefore must NOT |
|---|---|---|
| `ValidationBehaviour` | Runs FluentValidation, throws `ValidationException` (→ 400) before the handler | Re-validate what a validator already checks |
| `LoggingBehaviour` | Logs request name, duration, success/failure | Add its own timing logs |
| `AuditBehaviour` | Writes `audit_logs` for every command implementing `IAuditableCommand` | Call `context.AuditLogs.Add(...)` itself |

**Deliberate exceptions** (the only ones):

- The **public donor form** handler writes its own audit entry manually — there is no
  authenticated `user_id` for the pipeline to attach.
- `LogInteractionCommandHandler` is the only handler allowed to create follow-up reminder
  notifications via `INotificationService`. Any other handler doing this needs review.

### Handler rules

- Get the current user from `ICurrentUserService`, never `IHttpContextAccessor` and never
  `HttpContext.User` (those live in Infrastructure's `CurrentUserService`).
- Never set `entity.UpdatedAt` by hand — `UpdatedAtInterceptor` does it on `SaveChanges`.
- Depend only on the narrow repository interfaces in `Common/Interfaces/` + `IUnitOfWork`.
  Never a `DbSet<T>` or `IQueryable<T>`.
- Commit through `IUnitOfWork.SaveChangesAsync()`. All repositories and the UoW are
  request-scoped and share one `DbContext`, so multi-repository mutations commit atomically.
- Throw the typed exceptions from `Common/Exceptions/` (`NotFoundException`,
  `ForbiddenException`, `ConflictException`, `UnauthorizedException`, `ValidationException`).
  The middleware maps them to the standard envelope.

---

## 5. Persistence & EF Core

- **All EF Core lives in `CRM.Infrastructure`.** Repositories in
  `Persistence/Repositories/` are the only place `AsNoTracking()`, `Include()`,
  `ProjectTo<>()`, and LINQ-to-Entities appear. A query handler containing `AsNoTracking()`
  is itself the violation now — that query belongs in a repository.
- Every repository **read** method is `AsNoTracking()` **unless** its name and XML doc say
  it deliberately returns a tracked entity for mutation (e.g. `GetForUpdateAsync`).
- All entity → table mapping is Fluent API in `IEntityTypeConfiguration<T>`. No data
  annotations on entities.
- All enum properties use `.HasConversion<string>()` so PostgreSQL stores readable strings.
- Junction tables (`user_roles`, `donor_operational_regions`, `donor_donation_types`) use
  **composite** PKs via `builder.HasKey(x => new { x.DonorId, x.RegionId })` — not surrogate UUIDs.
- Indexes are added from day one on FKs, status fields, follow-up dates, and search fields.

### Migrations

```bash
dotnet ef migrations add <Name> --project src/CRM.Infrastructure --startup-project src/CRM.API
dotnet ef database update       --project src/CRM.Infrastructure --startup-project src/CRM.API
```

- **Never hand-edit a generated migration file.** If a migration contains SQL that
  `dotnet ef migrations add` wouldn't have produced, that's a red flag in review.
- The API runs `Database.MigrateAsync()` and the seeders on startup (except in `Testing`).
- Seed data is code, run in dependency order by `DatabaseSeeder`:
  `RoleSeeder` (the four roles — no others) → `LookupSeeder` (all dropdown data) →
  `AdminUserSeeder`.
- `AdminUserSeeder` creates exactly one bootstrap user — `admin@crm.local`, role
  `SuperAdmin` — only when the `users` table is empty. Its password is read from
  `ADMIN_DEFAULT_PASSWORD` (`.env` locally → `ChangeMe123!` in `.env.example`; Key Vault in
  prod), hashed with `PasswordHasher<User>`; the seeder throws if the value is unset. Never
  hardcode a password or seed a user from a migration. All other users are created through
  the user-management API.
- Every seeder is idempotent; `docker compose down -v` gives a clean slate.

---

## 6. Authentication

**Decision (final): no ASP.NET Core Identity infrastructure.**
`CrmDbContext : DbContext` only — not `IdentityDbContext`. `PasswordHasher<User>` is used
standalone; `UserManager<T>` / `SignInManager<T>` are not used. `User` and `Role` stay
plain domain entities with no framework coupling. Password policy and reset-token
generation are hand-rolled in the Application layer.

> The System Design Document §5.7 predates this decision and is being updated separately.
> Treat `.github/copilot-instructions.md` as authoritative on auth until then.

Rules:

- **Never confirm or deny account existence.**
  - `POST /api/auth/login` returns an **identical** message + `401` for "email not found"
    and "wrong password". The two paths must not diverge in wording.
  - `POST /api/auth/forgot-password` always returns `200` with the same generic message,
    whether or not the email exists.
- **JWT config is fixed:** HS256; access token 60 min; refresh token 7 days (absolute, from
  login). Refresh tokens are stored **only as SHA-256 hashes**, travel **only in the HttpOnly
  `crm_refresh` cookie** (never a JSON body), and **rotate on every refresh** — a rotated
  token presented again revokes its whole family. Logout marks the session **revoked** — not
  just rely on the client discarding it. Reset tokens are stored hashed and compared with
  `FixedTimeEquals`. Signing key comes from config/Key Vault, never hardcoded.
- **Password policy** is enforced by FluentValidation on every command that sets a password
  (register / reset / change): ≥ 8 chars, ≥ 1 digit, ≥ 1 uppercase, ≥ 1 special character.

---

## 7. Authorization

- Enforced **only** at the controller via `[Authorize(Policy = "…")]` (or `[Authorize(Roles = "…")]`).
  Never inside a handler, service, or validator. Role logic in the Application layer fails the PR.
- Policies (defined in `AuthorizationExtensions.AddCrmAuthorizationPolicies`):

  | Policy | Roles |
  |---|---|
  | `MarketingOrAbove` | Marketing, Procurement, Admin, SuperAdmin |
  | `ProcurementOrAbove` | Procurement, Admin, SuperAdmin |
  | `AdminOrAbove` | Admin, SuperAdmin |
  | `SuperAdminOnly` | SuperAdmin |

- Valid roles are exactly: `SuperAdmin`, `Admin`, `Procurement`, `Marketing`. No others exist.
- Unauthenticated endpoints (`/api/v1/public/donors/submit`, public lookups, forgot-password)
  must carry rate limiting — `[EnableRateLimiting("PublicFormPolicy")]` or the global limiter.
- Document-type restriction (BBBEE cert = Admin+) is applied with the
  `DocumentTypeAuthorizationFilter` / `DocumentTypeAuthorizationHandler`.

---

## 8. API contract

- **Route base:** `api/v1/...` for feature endpoints (`AuthController` currently uses
  `api/[controller]`).
- **List endpoints return the lightweight DTO only** — `DonorListItemDto`, never
  `DonorDetailDto`. Full detail loads only when a single record is opened.
- **Error envelope** — produced centrally by `ExceptionHandlingMiddleware`:

  ```json
  {
    "status": 400,
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "errors": [{ "field": "incomeTaxNumber", "message": "..." }],
    "traceId": "0HN..."
  }
  ```

  `traceId` is always `HttpContext.TraceIdentifier` — never null, never hardcoded.
  Codes in use: `NOT_FOUND` (404), `UNAUTHORIZED` (401), `VALIDATION_ERROR` (400),
  `FORBIDDEN` (403), `CONFLICT` (409), `RATE_LIMITED` (429), `INTERNAL_ERROR` (500).
  A 500 message is deliberately generic — real detail goes to Serilog only.
- **Public donor submission** returns a reference string in `DON-YYYY-NNNNN` format —
  never the donor `Guid`.
- **Document download URLs** are always SAS URLs from
  `IBlobStorageService.GenerateSasUrlAsync()` with **`TimeSpan.FromMinutes(15)`** max.
  A raw Azure Blob URL in any response fails the PR.
- Success bodies currently wrap the payload as `{ "data": ... }` for donor endpoints —
  follow the shape of the endpoints already in the controller you're editing.

---

## 9. Data rules

- **`income_tax_number` — validated at all three levels; never remove one:**
  1. Frontend — Zod `.refine(val => !val || !val.startsWith('4'), { message: '...' })`
  2. Application — FluentValidation rule in the command validator
  3. Database — `HasCheckConstraint()` in `DonorConfiguration`
  (Numbers starting with `4` are VAT numbers, not income-tax numbers.)
- **No hard deletes.** `context.*.Remove()` or `DELETE FROM` outside test code / migrations
  fails the PR. Documents & users soft-delete via `is_active = false`; donors move
  `PendingReview → Active → Lapsed` / `PendingReview → Rejected`.
- **Append-only:** never generate `UPDATE`/`DELETE` for `interaction_logs` or `audit_logs`;
  no `EntityState.Modified`, no `.Remove()`, no `UpdatedAt` on those entities.
- Timestamps are `TIMESTAMPTZ` in **UTC**; the frontend converts to SAST for display.
- POPIA: marketing consent is a boolean **plus** the exact consent datetime, per record.

---

## 10. Frontend conventions

Feature-first. Each feature under `src/features/<feature>/` owns its `components/`,
`hooks/`, `pages/`, and `schemas/`.

| Rule | Detail |
|---|---|
| **Axios** | Import `api` from `src/lib/axios.ts` only. `import axios from 'axios'` in feature code bypasses the JWT + 401-refresh interceptors — flagged. |
| **Routes** | Every path is a constant in `src/routes/paths.ts`. No hardcoded `navigate('/donors/123')`. |
| **Server state** | TanStack Query. Hooks named `use<Resource>` — `useDonors`, `useDonor`, `useCreateDonor`. All server data lives in these hooks. |
| **Mutations** | On success, invalidate the affected query: `queryClient.invalidateQueries({ queryKey: ['donors'] })`. |
| **Client state** | Zustand only — `authStore` (current user + access token, **in memory**), `uiStore` (UI prefs). No Redux. |
| **Storage** | **No `localStorage` / `sessionStorage` anywhere.** The access token is in memory; the refresh token is an HttpOnly cookie JavaScript can never read (XSS surface). A page reload restores the session by calling `refreshSession()` (`useHydrateAuth`) — never by persisting tokens in storage. Flagged on sight. |
| **Forms** | React Hook Form + Zod. Zod schemas live in the feature's `schemas/` folder, never inline in the component. |
| **shadcn/ui** | `components/ui/` is generated. Never hand-edit those files. |
| **RoleGuard / role checks** | Cosmetic only. Any security-sensitive action must also be `[Authorize]`-protected on the API. Frontend role checks are not security. |
| **Naming** | Components `PascalCase.tsx`; hooks `useThing.ts`; schemas `thing.schema.ts`; stores `thingStore.ts`; event handlers `handle*`; boolean props `is*`/`has*`. |

Match CI locally: `npx tsc --noEmit` and `npm run build` must both pass.

---

## 11. Testing standards

- **Every new handler and every new validator ships with unit tests.** No exceptions.
- Put handler/validator tests in `CRM.Application.Tests/Modules/<Module>/...`, mirroring
  the source folder.
- Repository / EF-config / interceptor tests go in `CRM.Infrastructure.Tests` (use
  `TestPostgres`). Controller / middleware / authorization tests go in `CRM.API.Tests`.
- Test method names: `Method_Scenario_ExpectedResult`.
- Frontend: co-locate `*.test.tsx` / `*.test.ts` next to the unit under test; Vitest + Testing Library.
- Run before every PR: `dotnet test CRM.slnx`, `npx tsc --noEmit`, frontend `npx vitest`.

---

## 12. Git workflow & Definition of Done

- Branch off **`Development`**. CI runs on `main` and `Development` for both push and PR.
- One logical change per PR. Link the GitHub issue.
- Commits: imperative mood, present tense ("Add donor approval handler").

**Definition of Done** (from `.github/copilot-instructions.md`):

- [ ] All acceptance criteria on the issue are checked off
- [ ] `dotnet build CRM.slnx` — **zero warnings**
- [ ] `npx tsc --noEmit` — zero errors
- [ ] Unit tests written for every new handler/validator
- [ ] `dotnet test CRM.slnx` — all green
- [ ] No [Non-Negotiable](#-non-negotiables) violated
- [ ] Backend CI **and** Frontend CI pass on the PR branch

**Review output:** every review opens with a Risk Assessment — `LOW` / `MEDIUM` / `HIGH`
plus a one-line reason. HIGH = any Non-Negotiable or auth/security rule touched;
MEDIUM = naming/convention breaks or missing tests on new handlers; LOW = docs/style only.

---

## 🔴 Non-Negotiables

A PR **fails** if any of these are violated:

1. `CRM.Domain` has a NuGet reference, or a `using` outside `System.*` / `CRM.Domain.*`.
2. `CRM.Application` references `Microsoft.EntityFrameworkCore` (any namespace, incl. the
   core package), `Azure.Storage.Blobs`, `SendGrid`, or `Microsoft.AspNetCore.*`.
3. `CRM.Application` references `CRM.Infrastructure`.
4. A controller contains business logic (`if`/`else`, loops, DB or service calls).
5. `interaction_logs` or `audit_logs` gets an `UPDATE`/`DELETE`/`.Remove()`, or an
   `UpdatedAt` property.
6. Login or forgot-password reveals whether an account exists.
7. JWT algorithm/expiry changed without explicit discussion; refresh token not revoked on logout;
   signing key hardcoded or committed.
8. A password-setting command missing any of the four policy checks.
9. A raw Azure Blob URL in any API response; or a SAS expiry over 15 minutes.
10. Role enforcement inside a handler / service / validator instead of the controller.
11. An unauthenticated endpoint with no rate limiting.
12. `HttpContext` / `ClaimsPrincipal` accessed from Application-layer code.
13. `income_tax_number` validation dropped at any of its three levels.
14. Any hard delete — `context.*.Remove()` or `DELETE FROM` outside tests/migrations.
15. A list endpoint returning the full detail DTO.
16. The public donor submission returning the donor `Guid` instead of a `DON-YYYY-NNNNN` reference.
17. A hand-edited EF migration file.
18. A handler manually writing `audit_logs` (outside the public-form exception) or manually
    setting `UpdatedAt`.
19. Frontend importing `axios` directly, using `localStorage`/`sessionStorage`, or hardcoding a route string.
