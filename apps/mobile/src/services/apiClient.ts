import { client } from './api/client.gen';
import { readPublicEnv, type PublicEnv } from './env';

/** Returns the current access token, if the member is signed in (Phase 03). */
export type AccessTokenProvider = () => string | undefined | Promise<string | undefined>;

let accessTokenProvider: AccessTokenProvider = () => undefined;

/** The auth service registers itself here; the client asks it for a token on each call. */
export function setAccessTokenProvider(provider: AccessTokenProvider): void {
  accessTokenProvider = provider;
}

/** Points the generated client at the API and attaches the bearer token where required. */
export function configureApiClient(env: PublicEnv = readPublicEnv()): void {
  client.setConfig({
    baseUrl: env.apiUrl,
    auth: () => accessTokenProvider(),
  });
}

/** An HTTP error from the API, with its status (used by the retry policy) and problem body. */
export class ApiError extends Error {
  override name = 'ApiError';

  constructor(
    readonly status: number,
    readonly problem?: unknown,
  ) {
    super(`API request failed with status ${status}`);
  }
}
