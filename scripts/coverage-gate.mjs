// Fails when any coverage metric of one project is below the threshold, and writes a
// Markdown table (to the GitHub job summary when GITHUB_STEP_SUMMARY is set).
//
// Usage: node scripts/coverage-gate.mjs <summary.json> <project label> [threshold]
// Accepts Istanbul's "json-summary" report (Jest: statements, branches, functions, lines)
// and ReportGenerator's "JsonSummary" report (.NET: lines and branches).
import { appendFileSync, readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export const METRICS = ['statements', 'branches', 'functions', 'lines'];
export const DEFAULT_THRESHOLD = 80;

const REPORTGENERATOR_METRICS = { lines: 'linecoverage', branches: 'branchcoverage' };

/** Percentages by metric name, from either report format. */
export function percentages(summary) {
  if (summary?.total) {
    return METRICS.map((metric) => {
      const pct = summary.total[metric]?.pct;
      if (typeof pct !== 'number') throw new Error(`Coverage summary has no "${metric}" percentage.`);
      return [metric, pct];
    });
  }
  if (summary?.summary) {
    return Object.entries(REPORTGENERATOR_METRICS).map(([metric, key]) => {
      const pct = summary.summary[key];
      if (typeof pct !== 'number') throw new Error(`Coverage summary has no "${key}" percentage.`);
      return [metric, pct];
    });
  }
  throw new Error('Coverage summary has no "total" section (Istanbul) or "summary" section (ReportGenerator).');
}

/** Returns one row per metric with its percentage and whether it passes. */
export function evaluate(summary, threshold = DEFAULT_THRESHOLD) {
  return percentages(summary).map(([metric, pct]) => ({ metric, pct, passed: pct >= threshold }));
}

export function toMarkdown(label, rows, threshold = DEFAULT_THRESHOLD) {
  const ok = rows.every((row) => row.passed);
  const lines = [
    `### ${ok ? 'Pass' : 'Fail'}: ${label} coverage (minimum ${threshold} %)`,
    '',
    '| Metric | Coverage | Status |',
    '| --- | ---: | --- |',
    ...rows.map(
      (row) => `| ${row.metric} | ${row.pct.toFixed(2)} % | ${row.passed ? 'pass' : 'below minimum'} |`,
    ),
    '',
  ];
  return lines.join('\n');
}

export function run(argv, env = process.env) {
  const [file, label, rawThreshold] = argv;
  if (!file || !label) {
    console.error('Usage: node scripts/coverage-gate.mjs <coverage-summary.json> <label> [threshold]');
    return 2;
  }
  const threshold = rawThreshold === undefined ? DEFAULT_THRESHOLD : Number(rawThreshold);
  if (!Number.isFinite(threshold)) {
    console.error(`Invalid threshold: ${rawThreshold}`);
    return 2;
  }
  const rows = evaluate(JSON.parse(readFileSync(file, 'utf8')), threshold);
  const markdown = toMarkdown(label, rows, threshold);
  console.log(markdown);
  if (env.GITHUB_STEP_SUMMARY) appendFileSync(env.GITHUB_STEP_SUMMARY, `${markdown}\n`);
  return rows.every((row) => row.passed) ? 0 : 1;
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
  process.exit(run(process.argv.slice(2)));
}
