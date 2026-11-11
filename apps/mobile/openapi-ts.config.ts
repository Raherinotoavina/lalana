import { defineConfig } from '@hey-api/openapi-ts';

/**
 * Generates the typed API client from the OpenAPI document that the API build writes to
 * api/openapi/. Run `npm run api:generate` after an API change; CI fails on drift.
 */
export default defineConfig({
  input: '../../api/openapi/lalana-api.json',
  output: 'src/services/api',
  plugins: ['@hey-api/client-fetch', '@hey-api/typescript', '@hey-api/sdk'],
});
