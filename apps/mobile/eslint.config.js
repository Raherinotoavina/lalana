const { defineConfig } = require('eslint/config');
const expoConfig = require('eslint-config-expo/flat');
const eslintPluginPrettierRecommended = require('eslint-plugin-prettier/recommended');

/** Only the auth service may talk to Supabase; everything else goes through the API. */
const SUPABASE_ONLY_IN_AUTH = {
  patterns: [
    {
      group: ['@supabase/*'],
      message:
        'Supabase is used only for Auth, from src/services/auth*. Call the API through src/services instead.',
    },
  ],
};

module.exports = defineConfig([
  expoConfig,
  eslintPluginPrettierRecommended,
  {
    ignores: ['dist/*', 'coverage/*', '.expo/*', 'expo-env.d.ts'],
  },
  {
    rules: {
      'no-restricted-imports': ['error', SUPABASE_ONLY_IN_AUTH],
    },
  },
  {
    files: ['src/services/auth.ts', 'src/services/auth/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': 'off',
    },
  },
]);
