/** Pre-commit checks on staged files only. The full suite runs on pre-push and in CI. */
const quote = (files) => files.map((file) => JSON.stringify(file)).join(' ');
const inMobile = (command) => `node scripts/run-in.mjs apps/mobile ${command}`;

export default {
  'apps/mobile/**/*.{ts,tsx,js,jsx,mjs,cjs}': (files) => [
    inMobile(`eslint --fix --max-warnings=0 --no-warn-ignored ${quote(files)}`),
    inMobile(`jest --bail --findRelatedTests --passWithNoTests ${quote(files)}`),
  ],
  'apps/mobile/**/*.{ts,tsx}': () => inMobile('tsc --noEmit'),
  'apps/mobile/**/*.{json,md}': (files) => inMobile(`prettier --write ${quote(files)}`),
  // dotnet format fixes style and analyzer issues in the staged C# files only.
  'api/**/*.cs': (files) => `dotnet format api/Lalana.slnx --include ${quote(files)}`,
};
