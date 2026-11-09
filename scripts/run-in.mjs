// Runs a locally installed tool inside one app's folder, so it finds that app's config.
// Usage: node scripts/run-in.mjs <dir> <tool> [args...]
import { spawnSync } from 'node:child_process';

const [dir, tool, ...args] = process.argv.slice(2);
if (!dir || !tool) {
  console.error('Usage: node scripts/run-in.mjs <dir> <tool> [args...]');
  process.exit(2);
}
const result = spawnSync('npx', ['--no-install', tool, ...args], { cwd: dir, stdio: 'inherit' });
process.exit(result.status ?? 1);
