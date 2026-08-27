# Copilot Instructions — SA Harvest CRM

## Project Overview

This is a donor management CRM for a South African non-profit (SA Harvest).
It replaces Monday.com. Built as a final year project.

- **Backend:** ASP.NET Core 9 Web API, C# 13, .NET 10
- **Architecture:** Modular Monolith + Clean Architecture (4 layers)
- **Database:** PostgreSQL 16 via Entity Framework Core 9 + Npgsql
- **Frontend:** React 19 + TypeScript 5 + Vite
- **State:** TanStack Query (server state) + Zustand (client state)
- **Forms:** React Hook Form + Zod validation
- **Components:** shadcn/ui + Tailwind CSS v4
- **Auth:** Standalone `PasswordHasher<User>` + JWT Bearer
  (NOT `IdentityDbContext`, NOT `UserManager<T>` — this decision is final
  and must not be reversed. Note: the System Design Document Section 5.7
  is being updated separately to reflect this; treat this file as the
  authoritative source on auth architecture in the meantime.)
- **File Storage:** Azure Blob Storage (SAS URLs only — never raw blob URLs)
- **Background Jobs:** Hangfire + Hangfire.PostgreSql
- **CQRS:** MediatR with three pipeline behaviours:
  LoggingBehaviour → ValidationBehaviour → AuditBehaviour

---

## PR Review Checklist

When reviewing a PR, check every item below. Call out violations
explicitly — do not let them pass because the code otherwise looks clean.

---

## 🔴 Non-Negotiables — Fail The PR If Any Of These Are Violated

### Clean Architecture Layer Rules

- `CRM.Domain` must have ZERO NuGet package references.
  If a using statement references anything other than System.*
  or another CRM.Domain namespace, flag it.

- `CRM.Application` must NOT reference:
  - Microsoft.EntityFrameworkCore (any namespace)
  - Azure.Storage.Blobs
  - SendGrid
  - Microsoft.AspNetCore (any namespace)
  If any of these appear in Application layer using statements, fail the PR.

- `CRM.Infrastructure` must NOT be referenced by `CRM.Application`.
  Only CRM.API references Infrastructure (for DI registration only).

- Controllers must be thin — no business logic.
  A controller method must only:
  1. Receive the HTTP request
  2. Map to a Command or Query
  3. Call _mediator.Send()
  4. Return the HTTP result
  If a controller contains if/else, loops, database calls,
  or direct service calls, fail the PR.

### Append-Only Tables

- `interaction_logs` must NEVER have UPDATE or DELETE statements
  generated against it anywhere in the codebase.
  No `context.InteractionLogs.Remove()`.
  No `entry.State = EntityState.Modified` for InteractionLog entities.

- `audit_logs` must NEVER have UPDATE or DELETE statements
  generated against it anywhere in the codebase.
  Same rules as interaction_logs.

- Neither table should have an `UpdatedAt` property on its entity.
  If either appears, flag it.

### Authentication & Security Rules

- **Never confirm or deny account existence.** This applies to both:
  - `POST /api/auth/login` — must return the identical error message and
    status code (401) for "email not found" and "password incorrect."
    Never let these two failure paths diverge in wording.
  - `POST /api/auth/forgot-password` — must always return 200 with the
    same generic message, regardless of whether the email exists in
    the system.
  If a PR adds a distinct error message for "user not found" vs.
  "wrong password" (or similar), fail the PR — this is a user
  enumeration vulnerability, not a UX nitpick.

- JWT token configuration must match these values. Flag any PR that
  changes them without an explicit, discussed reason:
  - Algorithm: HS256
  - Access token expiry: 60 minutes
  - Refresh token expiry: 7 days
  - Refresh tokens are stored in the database and MUST be invalidated
    (revoked) on logout — check that the logout handler actually
    marks the refresh token row as revoked/expired, not just that the
    client discards it.
  - Signing key must be read from configuration/Key Vault, never
    hardcoded or committed.

- Password policy must be enforced via FluentValidation on the
  registration/password-set commands (since Identity's built-in
  policy enforcement is not available without `UserManager<T>`):
  - Minimum 8 characters
  - At least one digit
  - At least one uppercase letter
  - At least one special character
  If a command that creates or resets a password is missing any of
  these checks, flag it.

- Raw Azure Blob Storage URLs must NEVER appear in any API response.
  Every document URL returned to a client must come from
  `IBlobStorageService.GenerateSasUrlAsync()` with a TimeSpan expiry.
  Check any response DTO or controller return value that includes
  a URL string — if it is not a SAS URL, fail the PR.

- Role enforcement must be on the controller method or class
  via `[Authorize(Roles = "...")]`.
  It must NOT be inside a handler, service, or validator.
  If role checking appears in Application layer code, fail the PR.
  Valid roles are: `SuperAdmin`, `Admin`, `Procurement`, `Marketing`.

- The public form endpoint (`/api/v1/public/donors/submit`) and any
  other unauthenticated endpoint must have rate limiting applied.
  Check for `[EnableRateLimiting("...")]` on public controllers.

- JWT claims must be read via `ICurrentUserService` only.
  Direct access to `HttpContext.User.Claims` or `ClaimsPrincipal`
  in Application layer code is a Clean Architecture violation.

### Data Rules

- `income_tax_number` validation must exist at ALL THREE levels:
  1. Frontend: Zod schema `.refine(val => !val.startsWith('4'))`
  2. Application: FluentValidation rule in the Command validator
  3. Database: CHECK constraint in DonorConfiguration via HasCheckConstraint()
  If a PR touches any of these three and removes or skips one level, flag it.

- No hard deletes. Ever. Entities are either:
  - Soft-deleted via `is_active = false` (documents, users)
  - Status-transitioned (donors: Active → Lapsed → Rejected)
  If `context.*.Remove()` or `DELETE FROM` appears outside of
  test code or migration files, fail the PR.

---

## 🟡 Naming and Type Conventions — Flag These As Required Changes

### C# Naming

- The domain entity for tasks is named `DonorTask`, not `Task`.
  `Task` conflicts with `System.Threading.Tasks.Task`.
  Any reference to a domain entity called `Task` (not `DonorTask`) is wrong.

- Status and type fields on domain entities must use enums, not strings:
  - `donors.Status` → `DonorStatus` enum
  - `donor_contacts.ContactType` → `ContactType` enum
  - `interaction_logs.InteractionType` → `InteractionType` enum
  - `donor_documents.DocumentType` → `DocumentType` enum
  - `donor_approvals.Status` → `ApprovalStatus` enum
  - `notifications.NotificationType` → `NotificationType` enum
  - `audit_logs.Action` → `AuditAction` enum
  If any of these are `string` properties on domain entities, flag it.

- Async methods must be suffixed with `Async`:
  `GetDonorByIdAsync`, `CreateDonorAsync`, etc.
  Non-async methods must NOT have the `Async` suffix.

- Private fields use `_camelCase` prefix: `_mediator`, `_context`.

### TypeScript/React Naming

- All route strings must come from `src/routes/paths.ts`.
  Hardcoded route strings like `navigate('/donors/123')` anywhere
  in component code should be flagged.

- TanStack Query hooks must be named with `use` prefix and describe
  the resource: `useDonors`, `useDonor`, `useCreateDonor`.

- Zod schemas must live in the feature's `schemas/` folder,
  not inline in the component file.

- The income_tax_number Zod rule must exist:
  `.refine(val => !val || !val.startsWith('4'), { message: '...' })`
  If the donor form schema is modified and this rule is missing, flag it.

---

## 🟡 API Contract Rules — Flag These

- List endpoints (GET /api/donors, GET /api/tasks etc.) must return the
  lightweight DTO only, never the full entity or full detail DTO.
  `DonorListItemDto` for donors — not `DonorDetailDto`.
  If a list handler returns the full detail shape, flag it.

- Every error response must follow the standard envelope:

```json
  {
    "status": 400,
    "code": "VALIDATION_ERROR",
    "message": "...",
    "errors": [{ "field": "...", "message": "..." }],
    "traceId": "..."
  }
```

  If a controller returns a plain string error or a non-standard
  shape, flag it. `traceId` must be populated from
  `HttpContext.TraceIdentifier` via the global exception middleware —
  never left null or hardcoded.

- The public donor form submission response must return a
  reference number string (DON-YYYY-NNNNN format), never the donor UUID.
  If the public endpoint returns the `Guid Id` of the created donor, fail it.

- SAS URLs for document downloads must use a maximum 15-minute expiry:
  `TimeSpan.FromMinutes(15)`.
  Longer expiries are a security issue — flag them.

---

## 🟡 EF Core and Database Rules — Flag These

- All entity-to-table mapping must use Fluent API in
  `IEntityTypeConfiguration<T>` classes in CRM.Infrastructure.
  Data Annotations (`[Required]`, `[MaxLength]`, `[Column]` etc.)
  on domain entity classes are a Clean Architecture violation —
  they couple the Domain layer to EF Core.

- `AsNoTracking()` must be called on all read (Query) operations.
  If a query handler loads entities without `AsNoTracking()`, flag it.

- **Where that rule now applies.** Since the repository refactor, no EF Core
  query lives in CRM.Application at all. `AsNoTracking()`, `Include()`,
  `ProjectTo<>()` and every other EF Core / LINQ-to-Entities construct belong
  exclusively to the repository implementations in
  `CRM.Infrastructure/Persistence/Repositories/`. Application-layer handlers and
  validators depend only on the narrow repository interfaces in
  `CRM.Application/Common/Interfaces/` plus `IUnitOfWork`; they never see a
  `DbSet<T>` or an `IQueryable<T>`. So read the rule as: every repository read
  method must be `AsNoTracking()` unless its name and XML doc say it deliberately
  returns a **tracked** entity for a caller to mutate (e.g. `GetForUpdateAsync`,
  `GetForMutationAsync`). A query handler containing `AsNoTracking()` is itself
  the violation now — the query belongs in a repository.

- **CRM.Application must have zero EF Core references — including the core
  `Microsoft.EntityFrameworkCore` package, not just provider packages.**
  There must be no `PackageReference` to any `Microsoft.EntityFrameworkCore.*`
  assembly in `CRM.Application.csproj`, and no `using Microsoft.EntityFrameworkCore;`
  anywhere under `src/CRM.Application/`. This is checkable with:
  `grep -rn "Microsoft.EntityFrameworkCore" src/CRM.Application/` → must be empty.
  There is deliberately no `IApplicationDbContext` abstraction any more: exposing
  `DbSet<T>` from the Application layer was what forced the EF Core dependency in
  the first place. Do not reintroduce it.

- All enum properties must use `.HasConversion<string>()`
  in the entity configuration so they are stored as readable
  strings in PostgreSQL, not integers.

- Migration files must not be manually edited.
  If a migration file contains hand-written SQL outside of
  what `dotnet ef migrations add` would generate, flag it.

- The `UpdatedAtInterceptor` handles `UpdatedAt` automatically.
  Handlers must NOT manually set `entity.UpdatedAt = DateTime.UtcNow`.
  If this appears in a handler, flag it.

---

## 🟡 MediatR and CQRS Rules — Flag These

- Handlers must NOT write to `audit_logs` directly.
  `AuditBehaviour` does this automatically for any command
  implementing `IAuditableCommand`.
  If a handler contains `context.AuditLogs.Add(...)`, flag it.
  The one deliberate exception is the public donor form submission
  handler, which writes its own audit entry manually since there is
  no authenticated `user_id` to attach via the normal pipeline.

- Handlers must NOT call `INotificationService` for follow-up reminders
  unless the handler is `LogInteractionCommandHandler` — that is the
  one deliberate exception. Any other handler creating follow-up
  reminder notifications should be reviewed carefully.

- ValidationBehaviour runs FluentValidation before every handler.
  Handlers must NOT re-validate input that a validator already covers.
  Duplicate validation in both a validator and a handler is noise.

- `ICurrentUserService` must be used to get the current user's ID
  in handlers, not constructor injection of `IHttpContextAccessor`.
  `IHttpContextAccessor` belongs in Infrastructure (`CurrentUserService.cs`)
  not in Application handlers.

---

## 🟡 Frontend Rules — Flag These

- Axios must be imported from `src/lib/axios.ts` (the configured instance),
  never from the `axios` package directly.
  Direct `import axios from 'axios'` in feature code bypasses the
  JWT interceptor and the 401 refresh handler.

- TanStack Query mutations that create or update data must invalidate
  the relevant query on success:
  `queryClient.invalidateQueries({ queryKey: ['donors'] })`
  If a mutation succeeds but the list does not refresh, flag it.

- `RoleGuard` in the frontend is cosmetic only.
  Any security-sensitive action (approve donor, download BBBEE cert)
  must also be protected by `[Authorize(Roles = "...")]` on the API.
  Frontend-only role checks are not security.

- No `localStorage` or `sessionStorage` usage anywhere.
  Auth tokens live in Zustand `authStore` (in-memory only).
  Browser storage is vulnerable to XSS. Flag any usage.

---

## 🟢 Context — Key Decisions Already Made (Do Not Re-Litigate)

These decisions are final. If a PR or comment attempts to reverse
any of these, note that the decision was made deliberately:

- **No IdentityDbContext** — `CrmDbContext : DbContext` only.
  `PasswordHasher<User>` is used standalone. `UserManager<T>` and
  `SignInManager<T>` are not used. This is intentional to keep
  `User` and `Role` as plain domain entities with no framework coupling.
  Password policy and reset-token generation are hand-rolled in the
  Application layer instead of relying on Identity's built-ins — see
  the Authentication & Security Rules section above.

- **No microservices** — Modular Monolith only.
  The module boundaries in `CRM.Application/Modules/` are the
  logical separation. They run in one process.

- **No Redux** — TanStack Query handles server state.
  Zustand handles client state (auth, UI preferences).
  Redux is not in the stack.

- **No SQL Server** — PostgreSQL only. Licensing cost was the
  deciding factor. This is not up for discussion.

- **No hard deletes** — ever. Soft deletes only.

- **Composite PKs on junction tables** — `donor_operational_regions`,
  `donor_donation_types`, and `user_roles` use composite PKs,
  not surrogate UUID keys. EF Core Fluent API handles this via
  `builder.HasKey(x => new { x.DonorId, x.RegionId })`.

- **DonorTask, not Task** — the entity name is DonorTask.
  This is permanent.

- **Four seeded roles** — `SuperAdmin`, `Admin`, `Procurement`,
  `Marketing`. Seeded by `RoleSeeder`. No other roles exist.

---

## 📁 Project Structure Reference

SA-Harvest-CRM/
├── src/
│ ├── CRM.Domain/ ← Entities, Enums. Zero dependencies.
│ ├── CRM.Application/ ← CQRS Handlers, Validators, Interfaces.
│ │ No EF Core. No Azure. No ASP.NET.
│ ├── CRM.Infrastructure/ ← EF Core, Azure, SendGrid, Hangfire.
│ ├── CRM.API/ ← Controllers, Middleware, DI wiring.
│ └── CRM.Web/ ← React + TypeScript frontend
│ └── src/
│ ├── features/ ← Feature-first. Each feature owns its
│ │ pages, components, hooks, schemas.
│ ├── components/ui/ ← shadcn/ui generated. NEVER edit these.
│ ├── lib/axios.ts ← Configured Axios instance. Always import
│ │ this, never raw axios.
│ ├── store/authStore.ts ← Zustand auth state.
│ └── routes/paths.ts ← All route strings. Never hardcode routes.
└── tests/
├── CRM.Application.Tests/ ← business logic unit tests
└── CRM.API.Tests/ ← controller/integration tests


Note: the frontend project folder is `CRM.Web` (PascalCase) — it was
renamed from `crm-web` early on. Watch for stale references to the old
lowercase name in scripts, Docker Compose, or CI cache paths.

---

## 📋 Definition of Done

A PR is ready to merge when:

- [ ] All acceptance criteria on the GitHub Issue are checked off
- [ ] Solution builds with zero warnings (`dotnet build`)
- [ ] TypeScript compiles with zero errors (`tsc --noEmit`)
- [ ] Unit tests written for any new handler or validator
- [ ] All tests pass (`dotnet test`)
- [ ] No items from the Non-Negotiables section above are violated
- [ ] CI pipeline passes on the PR branch

---

## 📊 Review Output Format

At the start of every review, before individual line comments, include a
short "Risk Assessment" summary with one of: LOW / MEDIUM / HIGH, plus a
one-line reason. Base this on:
- HIGH: any Non-Negotiable rule violated, or auth/security rule touched
- MEDIUM: naming/convention violations, or missing tests on new handlers
- LOW: purely stylistic or documentation-only changes