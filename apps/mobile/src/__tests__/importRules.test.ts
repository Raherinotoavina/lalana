/**
 * @jest-environment node
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';

const root = path.resolve(__dirname, '../..');
const eslintBin = path.join(root, 'node_modules', '.bin', 'eslint');

const SUPABASE =
  "import { createClient } from '@supabase/supabase-js';\nexport const c = createClient;\n";
const API_CLIENT = "import { getMe } from '@/services/api/sdk.gen';\nexport const m = getMe;\n";

type LintResult = { messages: { ruleId: string | null }[] };

/** Lints `source` as if it lived at `relativePath`, in a separate ESLint process. */
function restrictedImportErrors(source: string, relativePath: string): number {
  const run = spawnSync(
    eslintBin,
    ['--format', 'json', '--stdin', '--stdin-filename', path.join(root, relativePath)],
    { cwd: root, input: source, encoding: 'utf8' },
  );
  const [result] = JSON.parse(run.stdout) as LintResult[];
  return (result?.messages ?? []).filter((message) => message.ruleId === 'no-restricted-imports')
    .length;
}

describe('import rules', () => {
  jest.setTimeout(60_000);

  it('forbids Supabase everywhere, services included', () => {
    expect(restrictedImportErrors(SUPABASE, 'src/features/home/useTrips.ts')).toBe(1);
    expect(restrictedImportErrors(SUPABASE, 'src/services/auth.ts')).toBe(1);
  });

  it('forbids the generated API client outside src/services', () => {
    expect(restrictedImportErrors(API_CLIENT, 'src/features/account/useMe.ts')).toBe(1);
    expect(restrictedImportErrors(API_CLIENT, 'src/app/(tabs)/profile.tsx')).toBe(1);
  });

  it('allows the generated API client inside src/services', () => {
    expect(restrictedImportErrors(API_CLIENT, 'src/services/account.ts')).toBe(0);
  });
});
