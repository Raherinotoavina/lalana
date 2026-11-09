import { getMe } from './api/sdk.gen';
import type { MeResponse } from './api/types.gen';
import { ApiError } from './apiClient';

export type Me = MeResponse;

/**
 * GET /me: the signed-in member's account. Throws ApiError on an HTTP error; status 0 means
 * no HTTP response (network failure), which the query client retries.
 */
export async function fetchMe(): Promise<Me> {
  const { data, error, response } = await getMe();
  if (data === undefined) {
    throw new ApiError(response?.status ?? 0, error);
  }
  return data;
}
