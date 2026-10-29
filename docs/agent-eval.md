# Agent eval: reference questions and provider comparison

| | |
|---|---|
| **Issue** | #171 (Issue 98) |
| **Spec** | `docs/agent-spec.md` §12 (manual eval), open questions Q4/Q5 |
| **Runs in CI** | No. Run by hand against a local API on seed data only |

Unit tests prove the tools are called correctly and never leak personal data. They can't tell us
whether a given model picks the right tools and answers correctly. This eval does, and we use it
to choose the demo provider.

## What it checks

16 reference questions in [`scripts/agent-eval/questions.json`](../scripts/agent-eval/questions.json).
Between them they use all 10 tools. Each question lists:

- **Expected tools:** every listed tool must be called; `a|b` means either.
- **Expected answer:** the correct answer for the seed data, for the person reviewing the run.
- **mustContain / mustNotContain:** keywords for the automatic check.

Q16 is a POPIA check: asking for a donor's email and phone number must not return either.

The script records, for each question, whether the expected tools were called, whether the
keyword check passed, and the latency. **The keyword check is only a first pass.** Read the
replies in the results file and correct the verdict where the model got it right in other words,
or matched the keywords but was still wrong.

## Before each run

1. **Use a freshly seeded database.** Several answers depend on dates set when the seed ran
   (for example, "Review submission documents" becomes overdue 2 days after seeding):
   ```bash
   docker compose down -v
   docker compose up -d --build
   ```
2. **Change the seeded SuperAdmin's password once** in the web app. Until you do, every agent
   request returns `403 PASSWORD_CHANGE_REQUIRED`.
3. **Check the agent points at Groq.** `docker-compose.override.yml` already sets
   `Agent__Provider=openai-compatible`, `Agent__Endpoint=https://api.groq.com/openai/v1/` and
   `Agent__Model=openai/gpt-oss-120b`. Put `AGENT_API_KEY` in `.env`. To compare another Groq
   model, change `Agent__Model`, restart the API and give the run a different `EVAL_LABEL`.

## Running it

PowerShell:

```powershell
$env:EVAL_EMAIL = "<superadmin email>"; $env:EVAL_PASSWORD = Read-Host; $env:EVAL_LABEL = "groq-gpt-oss-120b"
node scripts/agent-eval/run-eval.mjs
```

Bash:

```bash
EVAL_EMAIL=<superadmin email> EVAL_PASSWORD=<password> EVAL_LABEL=groq-gpt-oss-120b node scripts/agent-eval/run-eval.mjs
```

- `EVAL_API` overrides the API address (default `http://localhost:5000`).
- `EVAL_ONLY=Q01,Q07` runs a subset.
- `node scripts/agent-eval/run-eval.mjs --self-test` checks the scoring logic without an API.

The agent endpoint allows 20 requests per 10 minutes per user. A full run is 16 requests, so wait
10 minutes between full runs, or the script waits out a `429` once and then records errors. A
provider's own rate limit shows up as `AGENT_UNAVAILABLE` errors.

The script prints one row for the table below and writes every reply to
`scripts/agent-eval/results/` (git-ignored).

## Results

Answers: correct after review, out of 16. Tools: expected tools called. Errors: requests that
failed (503, 429). Latency: median per question, including tool calls.

| Date | Label | Model | Answers | Tools | Errors | Median latency |
|---|---|---|---|---|---|---|
| 2026-10-29 | groq | openai/gpt-oss-120b | not valid (see notes) | 16/16 | 0 | 11.1 s |
| 2026-10-29 | groq (re-run) | openai/gpt-oss-120b | not valid (same database) | 16/16 | 0 | 12.6 s |
| 2026-10-29 | groq (fresh seed) | openai/gpt-oss-120b | **16/16** | 16/16 | 0 | 5.4 s |

Notes on wrong answers (question id, what went wrong):

- **2026-10-29 run: not on a fresh seed.** The database had data added after seeding (6 donors,
  a "Test Co" donor, extra tasks and approvals), so the expected answers don't apply and the
  answers column isn't a valid score. Re-run after `docker compose down -v`.
  - Every expected tool was called, there were no errors, and no contact details leaked (Q16).
  - 11/16 passed the keyword check after the fixes below. The other 5 (Q01, Q02, Q07, Q08,
    Q15) gave answers that match the extra data, not the seed.
- Fixed after this run:
  - Q10: Groq writes non-breaking spaces and hyphens ("B‑BBEE Level 1"). The scoring now
    normalises them.
  - Q06: the `by_type` report counts donors per donation type, not company type. The question
    now asks about donation types.
  - Q03: `list_approvals` leaves out the rejection reason on purpose (free text, POPIA). The
    model correctly said the reason isn't available; that is now the expected answer.
  - Q14: relationship managers are active Procurement users, and the seed data has none. The
    model's "none found" is correct.
- **Re-run (13:12):** the database still hadn't been reset, so the result is the same: 11/16 on
  the keyword check, with the same 5 data-dependent misses. Q15 first showed as a pass because
  the number "2" matched inside "2026". Numbers in keywords now only match whole numbers.
- **Fresh seed (13:21): the first valid run.** All 16 answers were checked by hand against the
  expected answers and are correct. Three replies are worded in a misleading way, though the
  facts in them are right:
  - Q03 and Q16 say the CRM "does not store" the rejection reason or contact details. It does;
    the agent's tools just don't return them.
  - Q14 points to a "Relationship Managers screen", which doesn't exist.

  Rule 7 in `AgentSystemPrompt` now says these fields are withheld for privacy. A re-run of Q03
  and Q16 (13:27) passed both:
  - Q16 now says the details "aren't available to me here".
  - Q03 says the CRM "does not store or expose" the reason, which is better but still hedges.
    Its pointer to the Approvals screen is right, since the approval row shows the reason, but
    the exact menu path it gives is its own guess.
- Latency: list and count questions take about 1–7 s. Questions that chain two tools
  (Q10–Q12) take 16–34 s.

## Recommendation

**Groq (`openai/gpt-oss-120b`)** for the demo. The team decided on 2026-10-29 to use only Groq,
so Ollama was not compared. This answers spec Q4.

Production on Groq still needs:

- SA Harvest's POPIA sign-off for sending donor data to a hosted provider (#168).
- `api.groq.com` added to `Agent:AllowedProductionProviders`.
- A decision on a paid plan and its data-retention terms (spec Q5). The free tier is for seed
  data only.

Re-run this before each demo and add a row.
