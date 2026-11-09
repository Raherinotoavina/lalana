import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';

import { evaluate, run, toMarkdown } from './coverage-gate.mjs';

const summary = (pcts) => ({
  total: Object.fromEntries(Object.entries(pcts).map(([metric, pct]) => [metric, { pct }])),
});
const PASSING = summary({ statements: 100, branches: 91.24, functions: 100, lines: 80 });
const FAILING = summary({ statements: 85, branches: 79.99, functions: 90, lines: 88 });

function writeSummary(content) {
  const dir = mkdtempSync(path.join(tmpdir(), 'coverage-gate-'));
  const file = path.join(dir, 'coverage-summary.json');
  writeFileSync(file, JSON.stringify(content));
  return { dir, file };
}

test('passes when every metric reaches the threshold (80 % included)', () => {
  assert.ok(evaluate(PASSING).every((row) => row.passed));
});

test('fails on a single metric below the threshold', () => {
  const failed = evaluate(FAILING).filter((row) => !row.passed);
  assert.deepEqual(
    failed.map((row) => row.metric),
    ['branches'],
  );
});

test('rejects a summary without totals or with a missing metric', () => {
  assert.throws(() => evaluate({}), /no "total" section/);
  assert.throws(() => evaluate({ total: { statements: { pct: 90 } } }), /no "branches"/);
  assert.throws(() => evaluate({ summary: { linecoverage: 90 } }), /no "branchcoverage"/);
});

test('reads ReportGenerator summaries (.NET): lines and branches', () => {
  const rows = evaluate({ summary: { linecoverage: 98.8, branchcoverage: 79.5, methodcoverage: 90 } });
  assert.deepEqual(
    rows.map((row) => [row.metric, row.passed]),
    [
      ['lines', true],
      ['branches', false],
    ],
  );
});

test('renders a Markdown table with the verdict', () => {
  const markdown = toMarkdown('Mobile', evaluate(FAILING));
  assert.match(markdown, /^### Fail: Mobile coverage \(minimum 80 %\)/);
  assert.match(markdown, /\| branches \| 79\.99 % \| below minimum \|/);
  assert.match(toMarkdown('Mobile', evaluate(PASSING)), /^### Pass/);
});

test('run returns 0 or 1 and appends to the job summary', () => {
  const { dir, file } = writeSummary(PASSING);
  const jobSummary = path.join(dir, 'summary.md');
  assert.equal(run([file, 'Mobile'], { GITHUB_STEP_SUMMARY: jobSummary }), 0);
  assert.match(readFileSync(jobSummary, 'utf8'), /Pass: Mobile coverage/);

  assert.equal(run([writeSummary(FAILING).file, 'Mobile'], {}), 1);
  assert.equal(run([writeSummary(FAILING).file, 'Mobile', '75'], {}), 0);
});

test('run rejects missing arguments and a bad threshold', () => {
  assert.equal(run([], {}), 2);
  assert.equal(run([writeSummary(PASSING).file, 'Mobile', 'eighty'], {}), 2);
});
