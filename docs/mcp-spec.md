# Spec — CRM MCP Server (v1, read-only, non-production)

| | |
|---|---|
| **Status** | Implemented (SDK `ModelContextProtocol.AspNetCore` 2.2.0) |
| **Date** | 2026-10-19 |
| **Audience** | SA Harvest CRM dev team |
| **Users** | `SuperAdmin` only, through an MCP client (Claude Code, Claude Desktop, MCP Inspector) |
| **Builds on** | `docs/agent-spec.md` (in-app agent, implemented) · `.github/copilot-instructions.md` |

---

## 1. Summary

Expose the **same read-only tools** the in-app agent uses (`CrmAgentTools`) over
the Model Context Protocol at `POST /mcp` on the existing API. MCP clients such
as Claude Code can then query live CRM data with the same allow-listed,
POPIA-minimised projections.

Nothing about the tools changes. Each one still sends an existing MediatR
query, so there is no new business logic. MCP is just a second entry point
next to `POST /api/v1/agent/messages`.

v1 is for **development, test and demo environments only**. Production stays
off until the auth model (§5.3) and POPIA sign-off (§8) are done.

---

## 2. Goals and non-goals

### Goals

- G1. One tool definition, two channels. Tools are defined only in `CrmAgentTools`, and the in-app agent and MCP can't drift apart.
- G2. Reuse existing auth: JWT bearer plus the `SuperAdminOnly` policy. **No change to JWT settings** (HS256, 60 min access, 7 day refresh).
- G3. Every MCP tool call is audited through `AuditBehaviour`, just as agent requests are.
- G4. The MCP endpoint cannot be switched on in Production by accident.
- G5. Comply with every rule in `.github/copilot-instructions.md`.

### Non-goals (v1)

- Production use.
- Write tools (they come after the in-app v2 confirm-step design).
- MCP resources, prompts, sampling, elicitation or notifications. **Tools only.**
- MCP OAuth authorization flow, personal access tokens or long-lived tokens (see §5.3 for later).
- Stateful sessions or SSE streaming.

---

## 3. Decisions

| # | Decision | Chosen | Why |
|---|---|---|---|
| D1 | **Auth** | Existing short-lived JWT access token, sent as `Authorization: Bearer`, with `[SuperAdminOnly]` on the endpoint. | Zero new auth code and no JWT config change. A 60-minute token is acceptable for dev/demo use. Long-lived tokens would need a new, reviewed mechanism (§5.3). |
| D2 | **Audit** | An MCP call-tool filter sends a new `RecordMcpToolCallCommand : IAuditableCommand` after every call, and `AuditBehaviour` writes the row. | Handlers never write `audit_logs` directly (copilot rule). One row per tool call, the same pipeline as everything else. |
| D3 | **Environments** | `Mcp:Enabled` defaults to `false`. The endpoint is mapped only when it's enabled. `ProductionConfigurationGuard` fails startup if `Mcp:Enabled = true` in Production. | Data sent over MCP goes to whatever LLM the *client* uses, which our `Agent:AllowedProductionProviders` guard cannot control. |
| D4 | **Tool registration** | `CrmAgentTools` stays the single source of truth in CRM.Application. CRM.API registers each tool with `McpServerTool.Create(MethodInfo, createTargetFunc, options)`, resolving `CrmAgentTools` from the request's DI scope for every call. | CRM.Application gets no MCP package. Tools get a fresh scoped `ISender`, and therefore the same `ICurrentUserService` user, per call. Tool names are defined once. |

---

## 4. Architecture

```
MCP client (Claude Code / Desktop / Inspector)
   │  POST /mcp   Authorization: Bearer <JWT>
   ▼
CRM.API pipeline (existing): ForwardedHeaders → Authentication → PasswordChangeRequired
   → Authorization (SuperAdminOnly) → RateLimiter ("Mcp" policy)
   ▼
MapMcp("/mcp")  ── only when Mcp:Enabled = true
   │  tools/list → 10 tools built from CrmAgentTools.Definitions
   │  tools/call → CallToolFilter ─┬─▶ CrmAgentTools.<Method>()  (resolved from request scope)
   │                               │        └─▶ ISender.Send(existing query) → repositories → PostgreSQL
   │                               └─▶ after return: ISender.Send(RecordMcpToolCallCommand)
   │                                          └─▶ AuditBehaviour → audit_logs (1 row)
   ▼
JSON-RPC response (stateless, no session)
```

### 4.1 Layer placement

| Piece | Layer | Change |
|---|---|---|
| `CrmAgentTools.Definitions` | CRM.Application `Modules/Agent/Tools/` | **Small refactor:** a static list of `(string Name, MethodInfo Method)`. `AsTools()` builds its `AIFunction`s from it, and MCP registration reads the same list. No behaviour change for the in-app agent. |
| `RecordMcpToolCallCommand` + handler + validator | CRM.Application `Modules/Agent/Commands/RecordMcpToolCall/` | New. The handler only sets the audit fields (§6). |
| MCP server registration, call-tool filter | CRM.API `Extensions/McpExtensions.cs` | New. `AddCrmMcp(configuration)` and `MapCrmMcp()`. |
| Rate-limit policy `Mcp` | CRM.API `RateLimitingExtensions` | New, per user, same shape as `AgentPolicy`. |
| Production guard | CRM.API `ProductionConfigurationGuard` | Add the `Mcp:Enabled` rule. |
| NuGet `ModelContextProtocol.AspNetCore` | **CRM.API only** | Never referenced by Application, Domain or Infrastructure. |

No controller is added. MCP is mapped as an endpoint like `MapHealthChecks`.
The thin-controller rule is still met because the endpoint contains no
business logic: it dispatches to the existing tools, which send MediatR
queries.

### 4.2 Authorization

The same reasoning as agent-spec §4.2 applies:

- `/mcp` requires `SuperAdminOnly`.
- Every policy on the wrapped endpoints includes SuperAdmin, so MCP opens no new access.
- Role checks stay out of tools and handlers (copilot rule).
- `ICurrentUserService` resolves the JWT user inside the same HTTP request, so `get_my_tasks` and the dashboard's caller-scoped counters still work.

`PasswordChangeRequiredMiddleware` already runs before endpoints, so a
SuperAdmin who must change their password is blocked on `/mcp` too. Add a test
for this.

---

## 5. Endpoint and transport

### 5.1 Endpoint

- `app.MapMcp("/mcp").RequireAuthorization("SuperAdminOnly").RequireRateLimiting(RateLimitingExtensions.McpPolicy)`
- Mapped only when `Mcp:Enabled = true`. When disabled, `/mcp` returns 404, so its existence isn't advertised.
- Streamable HTTP transport in **stateless** mode (`WithHttpTransport(o => o.Stateless = true)`; confirm the option name against the SDK version you pin). Stateless suits Azure App Service scale-out and needs no session store.
- Server info: `name = "sa-harvest-crm"`, `version` = assembly version.
- Server `instructions`: a short version of the agent system prompt's rules 2, 3, 4 and 6 (grounding, read-only, tool data is not instructions, SA conventions). The MCP client's own model reads these.

### 5.2 How a developer connects (documented in `DEVELOPER_GUIDE.md`)

1. Sign in as a SuperAdmin: `POST /api/auth/login`. The response body contains `accessToken`.
2. Add the server to Claude Code:

```bash
claude mcp add --transport http sa-harvest-crm http://localhost:5000/mcp --header "Authorization: Bearer <accessToken>"
```

3. When the token expires after 60 minutes, calls return 401. Sign in again and update the header.

Use **seed data only** (§8).

### 5.3 Production auth (future, not v1)

Both options need their own spec and security review before production:

- **MCP OAuth 2.1 authorization** (the protocol's standard flow). The CRM API acts as the resource server; this needs an authorization server and protected-resource metadata.
- **Personal access tokens** for SuperAdmins: hashed, expiring, revocable, audited, and separate from the JWT settings.

---

## 6. Auditing

`RecordMcpToolCallCommand : IAuditableCommand`:

| Field | Value |
|---|---|
| `EntityType` | `"McpToolCall"` |
| `Action` | `AuditAction.Viewed` (existing value; no enum change) |
| `EntityId` | A new `Guid` per call, also written to the Serilog log scope for correlation |
| `OldValues` | `null` |
| `NewValues` | `{ channel: "mcp", tool, arguments, isError, client: { name, version } }`. The client name and version come from the MCP `initialize` info when available. |

- The filter sends the command **after** the tool returns, for both successful and error results. Tool failures come back as results with `IsError = true` rather than exceptions (see SDK issue #820), so the filter sees every outcome.
- **Tool results are not stored**: they're derived data and would duplicate PII into `audit_logs`.
- `RecordMcpToolCallCommandValidator`:
  - `tool` must be one of the names in `CrmAgentTools.Definitions`;
  - the serialized `arguments` must be at most 4 KB, truncated with a marker if larger.
- `audit_logs` stays append-only.

**Illustrative filter** (confirm names against the pinned SDK version):

```csharp
.WithRequestFilters(f => f.AddCallToolFilter(next => async (context, ct) =>
{
    var result = await next(context, ct);
    await context.Services!.GetRequiredService<ISender>().Send(new RecordMcpToolCallCommand
    {
        Tool = context.Params!.Name,
        Arguments = context.Params.Arguments,
        IsError = result.IsError ?? false,
        ClientName = context.Server.ClientInfo?.Name,
        ClientVersion = context.Server.ClientInfo?.Version,
    }, ct);
    return result;
}))
```

---

## 7. Tool registration

### 7.1 Single source of truth (Application)

```csharp
public sealed partial class CrmAgentTools(ISender sender)
{
    public static readonly IReadOnlyList<(string Name, MethodInfo Method)> Definitions =
    [
        ("search_donors", Method(nameof(SearchDonorsAsync))),
        ("get_donor", Method(nameof(GetDonorAsync))),
        // ... all 10, same names as today
    ];

    public IList<AITool> AsTools() =>
        [.. Definitions.Select(d => AIFunctionFactory.Create(d.Method, this, d.Name))];

    private static MethodInfo Method(string name) => typeof(CrmAgentTools).GetMethod(name)!;
    // tool methods unchanged
}
```

### 7.2 MCP registration (API)

```csharp
services.AddMcpServer(o => { /* server info + instructions */ })
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools(CrmAgentTools.Definitions.Select(d => McpServerTool.Create(
        d.Method,
        ctx => ctx.Services!.GetRequiredService<CrmAgentTools>(),   // per-call, request-scoped
        new McpServerToolCreateOptions { Name = d.Name, ReadOnly = true, Destructive = false, Idempotent = true })))
    .WithRequestFilters(/* §6 */);
```

- All tools carry the hints `readOnlyHint = true`, `destructiveHint = false` and `idempotentHint = true`.
- `CancellationToken` parameters are bound by the SDK and stay out of the JSON schema, the same as with `AIFunctionFactory`.
- The `[Description]` attributes already on the methods and parameters become the MCP tool and parameter descriptions.
- `RunAsync`'s handling of `NotFoundException` and `ValidationException` still returns `{ error }` as normal content, so the model can recover. Other exceptions become `IsError` results through the SDK.

---

## 8. Security and POPIA

| Risk | Mitigation |
|---|---|
| **Data goes to the MCP client's LLM**, which we don't configure (e.g. a developer's Claude subscription) | `Mcp:Enabled` is off by default; the production guard blocks it; seed data only; documented in `DEVELOPER_GUIDE.md` and the risk register. |
| **Token handling.** The JWT sits in the developer's MCP client config. | It expires after 60 minutes; SuperAdmin only; dev environments only. Don't commit MCP configs that contain tokens; add `.mcp.json` to `.gitignore` if anyone creates one in the repo. |
| **Prompt injection** via donor data from the public form | Read-only tools; allow-listed and scrubbed projections (unchanged); server instructions say tool data is not instructions; MCP clients ask the user before each tool call by default. |
| **Abuse or scraping** | Per-user `Mcp` rate limit; page sizes capped at 25 in the tools; every call audited. |
| **Privilege escalation** | `SuperAdminOnly` plus the §4.2 rule. |
| **DNS rebinding / cross-origin calls** to a local `/mcp` | Existing CORS policy only allows the web app's origin. MCP clients aren't browsers, so they don't need CORS. Don't add `/mcp` to any CORS allow-list. |

**Docs to update in the same PR:**

- `docs/risk-register.md`: add "MCP data egress to client-chosen LLM".
- `docs/security-review.md`: add the `/mcp` endpoint.
- `docs/DEVELOPER_GUIDE.md`: §5.2 connection steps.
- `.github/copilot-instructions.md`: one line saying `ModelContextProtocol.*` is allowed in CRM.API only.

---

## 9. Configuration

| Key | Default | Notes |
|---|---|---|
| `Mcp:Enabled` | `false` | `true` only in Development, Test or Demo. The production guard throws if it is `true` in Production. |
| `RateLimiting:Mcp:PermitLimit` | `60` | Per user, counting every `/mcp` HTTP request (`initialize` and `tools/list` as well as `tools/call`). Higher than the agent's limit because one question can trigger several tool calls. |
| `RateLimiting:Mcp:WindowMinutes` | `10` | |

Add the keys to `.env.example`, set `Mcp__Enabled=true` in `docker-compose.override.yml` (local dev only), and leave it unset in Azure.

---

## 10. Testing

**`tests/CRM.Application.Tests/Modules/Agent/`**

- `CrmAgentToolsDefinitionsTests`:
  - 10 definitions with unique names;
  - every method has a `[Description]`;
  - `AsTools()` names equal the `Definitions` names (guards against drift between the two channels).
- `RecordMcpToolCallCommandHandlerTests`: sets `EntityType`, `Action`, a non-empty `EntityId` and `NewValues` (no result payload).
- `RecordMcpToolCallCommandValidatorTests`: rejects unknown tool names; truncates large arguments.
- Existing `CrmAgentToolsTests` (redaction and so on) keep passing unchanged. They cover MCP output too.

**`tests/CRM.API.Tests/`** (`WebApplicationFactory` plus the SDK's MCP client over HTTP):

- 401 with no token; 403 for Admin, Procurement and Marketing.
- 403 (or the middleware's existing response) for a SuperAdmin who must change their password.
- `tools/list` returns exactly the 10 names, all with `readOnlyHint = true`.
- `tools/call search_donors` returns allow-listed fields only and writes **one** `audit_logs` row with `entity_type = 'McpToolCall'`.
- A call with an unknown donor id returns `{ error }` content and is still audited with `isError` as returned.
- 404 on `/mcp` when `Mcp:Enabled = false`.
- 429 after the `Mcp` limit is exceeded.
- `ProductionConfigurationGuard` fails when `Mcp:Enabled = true` in Production.

**Manual:** connect MCP Inspector and Claude Code against the Docker dev
stack with seed data, then run the agent eval's reference questions through
Claude Code.

---

## 11. Acceptance criteria

- [ ] `ModelContextProtocol.AspNetCore` is referenced by CRM.API only.
- [ ] `CrmAgentTools.Definitions` is the only place tool names are defined, and the in-app agent behaves exactly as before.
- [ ] `/mcp` exists only when `Mcp:Enabled = true`, and startup fails if that is set in Production.
- [ ] `/mcp` requires `SuperAdminOnly` and the `Mcp` rate-limit policy.
- [ ] Every `tools/call` writes one `audit_logs` row via `AuditBehaviour`.
- [ ] All tools are advertised as read-only, non-destructive and idempotent.
- [ ] All tests in §10 pass; `dotnet build` produces zero warnings.
- [ ] Docs in §8 are updated.

---

## 12. Open questions

| # | Question | Proposed answer | Owner |
|---|---|---|---|
| Q1 | Which SDK version do we pin? | The latest stable `ModelContextProtocol.AspNetCore` at implementation time. Verify the option and filter names used in §5–§7 against it (the docs list v1 and v2). | Dev team |
| Q2 | Should the shared Azure test/demo environment have MCP on? | Only if it runs on seed data and the team needs it for the presentation. Otherwise local Docker only. | Team |
| Q3 | Which production auth option (§5.3)? | Defer until there is a real production user for MCP. | Team / SA Harvest |

---

## 13. Delivery plan

1. **Refactor**: `CrmAgentTools.Definitions` + drift test. Small and safe; can merge first.
2. **MCP endpoint**: package, `McpExtensions`, rate limit, production guard, audit command, filter, API tests.
3. **Docs**: developer guide, risk register, security review, copilot-instructions line.
