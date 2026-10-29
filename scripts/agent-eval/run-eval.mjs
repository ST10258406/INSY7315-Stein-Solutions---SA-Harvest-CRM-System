#!/usr/bin/env node
// Manual agent eval (Issue 98, docs/agent-eval.md). Not run in CI.
// Sends each reference question to POST /api/v1/agent/messages as a SuperAdmin and records
// tool use, latency and an automatic keyword check. A person still confirms each answer.
//
//   EVAL_EMAIL=... EVAL_PASSWORD=... EVAL_LABEL=groq-gpt-oss-120b node scripts/agent-eval/run-eval.mjs
//   node scripts/agent-eval/run-eval.mjs --self-test
//
// Env: EVAL_API (default http://localhost:5000), EVAL_EMAIL, EVAL_PASSWORD, EVAL_LABEL (results file
// name), EVAL_ONLY (comma-separated ids, e.g. Q01,Q07).
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { randomUUID } from 'node:crypto';
import assert from 'node:assert/strict';

const here = dirname(fileURLToPath(import.meta.url));

/** 'a|b' entries match if either alternative matches. */
const anyOf = (entry, test) => entry.split('|').some(test);

// Models write non-breaking spaces and hyphens ("B‑BBEE Level 1"); fold them so keywords still match.
const normalise = (s) => s.toLowerCase().replace(/[‐-―]/g, '-').replace(/\s+/g, ' ');

export function score(q, reply, toolCalls) {
  const text = normalise(reply);
  // A bare number must match a whole number, so "2" doesn't match inside "2026".
  const has = (s) => (/^\d+$/.test(s) ? new RegExp(`(?<![\\d.,])${s}(?![\\d.,]\\d|\\d)`).test(text) : text.includes(normalise(s)));
  const called = new Set(toolCalls);
  const toolsOk = q.expectedTools.every((entry) => anyOf(entry, (t) => called.has(t)));
  const containsOk = (q.mustContain ?? []).every((entry) => anyOf(entry, has));
  const leaked = (q.mustNotContain ?? []).filter(has);
  return { toolsOk, answerOk: containsOk && leaked.length === 0, leaked };
}

function selfTest() {
  const q = { expectedTools: ['search_donors', 'get_donor|run_report'], mustContain: ['Level 1', 'JHB|Johannesburg'], mustNotContain: ['@'] };
  assert.deepEqual(score(q, 'Level 1, based in Johannesburg', ['search_donors', 'run_report']), { toolsOk: true, answerOk: true, leaked: [] });
  assert.equal(score(q, 'Level 1, JHB', ['get_donor']).toolsOk, false);
  assert.equal(score(q, 'level 1 in jhb, mail a@b.co', ['search_donors', 'get_donor']).answerOk, false);
  assert.equal(score({ expectedTools: [], mustContain: [] }, 'anything', []).answerOk, true);
  assert.equal(score({ expectedTools: [], mustContain: ['B-BBEE Level 1'] }, 'B‑BBEE Level 1', []).answerOk, true);
  const two = { expectedTools: [], mustContain: ['2'] };
  assert.equal(score(two, 'contacted 1 donor up to 29 Oct 2026', []).answerOk, false);
  assert.equal(score(two, 'contacted **2** donors', []).answerOk, true);
  assert.equal(score(two, 'contacted 2.', []).answerOk, true);
  assert.equal(score(two, 'R2.50', []).answerOk, false);
  console.log('self-test passed');
}

async function post(url, body, token) {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    // non-JSON error page; keep the raw text below
  }
  return { status: res.status, json, text, retryAfter: Number(res.headers.get('retry-after')) || 0 };
}

async function ask(api, token, question) {
  const body = { conversationId: randomUUID(), messages: [{ role: 'user', content: question }] };
  let res = await post(`${api}/api/v1/agent/messages`, body, token);
  if (res.status === 429 && res.retryAfter) {
    console.log(`  rate limited; waiting ${res.retryAfter}s and retrying once`);
    await new Promise((r) => setTimeout(r, res.retryAfter * 1000));
    res = await post(`${api}/api/v1/agent/messages`, body, token);
  }
  return res;
}

async function main() {
  const api = (process.env.EVAL_API ?? 'http://localhost:5000').replace(/\/$/, '');
  const { EVAL_EMAIL: email, EVAL_PASSWORD: password } = process.env;
  if (!email || !password) throw new Error('Set EVAL_EMAIL and EVAL_PASSWORD (a SuperAdmin on the seeded dev database).');
  const label = process.env.EVAL_LABEL ?? 'unlabelled';
  const only = process.env.EVAL_ONLY?.split(',').map((s) => s.trim());

  const login = await post(`${api}/api/auth/login`, { email, password }).catch(() => {
    throw new Error(`Can't reach the API at ${api}. Start it (docker compose up -d) or set EVAL_API.`);
  });
  if (login.status !== 200 || !login.json?.accessToken) throw new Error(`Login failed (${login.status}): ${login.text.slice(0, 300)}`);
  const token = login.json.accessToken;

  const { questions } = JSON.parse(readFileSync(join(here, 'questions.json'), 'utf8'));
  const results = [];
  for (const q of questions.filter((q) => !only || only.includes(q.id))) {
    process.stdout.write(`${q.id} ${q.question}\n`);
    const started = performance.now();
    const res = await ask(api, token, q.question);
    const latencyMs = Math.round(performance.now() - started);

    if (res.status !== 200) {
      const code = res.json?.code ?? res.status; // standard error envelope: { status, code, message, ... }
      if (code === 'PASSWORD_CHANGE_REQUIRED') throw new Error('This account must change its password first. Log in to the web app once and change it.');
      results.push({ id: q.id, question: q.question, error: String(code), latencyMs });
      console.log(`  ERROR ${code} (${latencyMs} ms)`);
      continue;
    }

    const toolCalls = (res.json.toolCalls ?? []).map((t) => t.name);
    const verdict = score(q, res.json.reply ?? '', toolCalls);
    results.push({ id: q.id, question: q.question, model: res.json.model, latencyMs, toolCalls, ...verdict, expectedAnswer: q.expectedAnswer, reply: res.json.reply });
    console.log(`  tools ${verdict.toolsOk ? 'ok' : 'MISS'} [${toolCalls.join(', ')}] | answer ${verdict.answerOk ? 'ok' : 'CHECK'} | ${latencyMs} ms`);
  }

  const answered = results.filter((r) => !r.error);
  const latencies = answered.map((r) => r.latencyMs).sort((a, b) => a - b);
  const summary = {
    label,
    model: answered[0]?.model ?? null,
    runAt: new Date().toISOString(),
    questions: results.length,
    errors: results.length - answered.length,
    toolsOk: answered.filter((r) => r.toolsOk).length,
    answerOk: answered.filter((r) => r.answerOk).length,
    medianLatencyMs: latencies[Math.floor(latencies.length / 2)] ?? null,
  };

  const outDir = join(here, 'results');
  mkdirSync(outDir, { recursive: true });
  const outFile = join(outDir, `${label}-${summary.runAt.replace(/[:.]/g, '-')}.json`);
  writeFileSync(outFile, JSON.stringify({ summary, results }, null, 2));

  console.log('\nRow for docs/agent-eval.md (answer column is the automatic check; confirm it by reading the replies):');
  console.log(`| ${summary.runAt.slice(0, 10)} | ${label} | ${summary.model} | ${summary.answerOk}/${summary.questions} | ${summary.toolsOk}/${summary.questions} | ${summary.errors} | ${summary.medianLatencyMs} ms |`);
  console.log(`\nFull replies: ${outFile}`);
}

const isCli = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
if (isCli && process.argv.includes('--self-test')) selfTest();
else if (isCli) main().catch((err) => {
  console.error(err.message);
  process.exit(1);
});
