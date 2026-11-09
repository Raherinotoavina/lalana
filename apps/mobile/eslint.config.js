const { defineConfig } = require('eslint/config');
const expoConfig = require('eslint-config-expo/flat');
const eslintPluginPrettierRecommended = require('eslint-plugin-prettier/recommended');

const NO_SUPABASE = {
  group: ['@supabase/*'],
  message: 'There is no Supabase: the app talks to the Lalana API only.',
};

/** Screens and features use TanStack Query hooks; only src/services touches the API client. */
const API_CLIENT_ONLY_IN_SERVICES = {
  group: ['@/services/api/*', '**/services/api/*'],
  message:
    'Use the TanStack Query hooks of src/features, which call src/services. Only src/services imports the generated API client.',
};
module.exports = defineConfig([
  expoConfig,
  eslintPluginPrettierRecommended,
  {
    ignores: ['dist/*', 'coverage/*', '.expo/*', 'expo-env.d.ts'],
  },
  {
    rules: {
      'no-restricted-imports': ['error', { patterns: [NO_SUPABASE, API_CLIENT_ONLY_IN_SERVICES] }],
    },
  },
  {
    files: ['src/services/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', { patterns: [NO_SUPABASE] }],
    },
  },
]);
