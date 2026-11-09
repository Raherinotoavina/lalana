import { QueryClient, focusManager } from '@tanstack/react-query';
import { AppState, Platform, type AppStateStatus } from 'react-native';

/**
 * Server state lives in TanStack Query. Screens call hooks from `src/features/<domain>`,
 * which call `src/services/<domain>.ts` (the generated API client). Nothing calls `fetch`
 * directly from a screen.
 */

export const MAX_QUERY_RETRIES = 2;

/** HTTP status carried by an API error, if any. */
export function statusOf(error: unknown): number | undefined {
  if (typeof error !== 'object' || error === null || !('status' in error)) return undefined;
  const { status } = error as { status: unknown };
  return typeof status === 'number' ? status : undefined;
}

/**
 * Retry network failures and server errors (a weak 3G link drops requests), but not
 * client errors: a 401 or 404 will not fix itself. 408 and 429 are worth retrying.
 */
export function shouldRetry(failureCount: number, error: unknown): boolean {
  const status = statusOf(error);
  const clientError = status !== undefined && status >= 400 && status < 500;
  if (clientError && status !== 408 && status !== 429) return false;
  return failureCount < MAX_QUERY_RETRIES;
}

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        retry: shouldRetry,
      },
      // Writes (bookings, payments) are never retried blindly; the API makes them idempotent
      // and the screen decides whether to try again.
      mutations: { retry: false },
    },
  });
}

/** React Native has no window focus: refetch stale queries when the app comes back. */
export function handleAppStateChange(status: AppStateStatus): void {
  if (Platform.OS !== 'web') focusManager.setFocused(status === 'active');
}

export function subscribeToAppFocus(): () => void {
  const subscription = AppState.addEventListener('change', handleAppStateChange);
  return () => subscription.remove();
}
