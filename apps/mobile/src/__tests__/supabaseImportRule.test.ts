/**
 * @jest-environment node
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';

const root = path.resolve(__dirname, '../..');
const eslintBin = path.join(root, 'node_modules', '.bin', 'eslint');
const SOURCE =
  "import { createClient } from '@supabase/supabase-js';\nexport const client = createClient;\n";

type LintResult = { messages: { ruleId: string | null }[] };

/** Lints SOURCE as if it lived at `relativePath`, in a separate ESLint process. */
function restrictedImportErrors(relativePath: string): number {
  const run = spawnSync(
    eslintBin,
    ['--format', 'json', '--stdin', '--stdin-filename', path.join(root, relativePath)],
    { cwd: root, input: SOURCE, encoding: 'utf8' },
  );
  const [result] = JSON.parse(run.stdout) as LintResult[];
  return (result?.messages ?? []).filter((message) => message.ruleId === 'no-restricted-imports')
    .length;
}

describe('Supabase import rule', () => {
  jest.setTimeout(60_000);

  it('fails lint when a screen, feature or other service imports Supabase', () => {
    expect(restrictedImportErrors('src/features/home/useTrips.ts')).toBe(1);
    expect(restrictedImportErrors('src/services/trips.ts')).toBe(1);
  });

  it('allows the auth service', () => {
    expect(restrictedImportErrors('src/services/auth.ts')).toBe(0);
    expect(restrictedImportErrors('src/services/auth/session.ts')).toBe(0);
  });
});
