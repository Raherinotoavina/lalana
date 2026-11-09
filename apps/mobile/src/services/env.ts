/**
 * Public configuration embedded in the app. Everything here ships inside the app bundle
 * and is readable by anyone: never put a secret in an EXPO_PUBLIC_ variable.
 */

export type PublicEnv = {
  apiUrl: string;
};

export type RawPublicEnv = { [K in keyof PublicEnv]: string | undefined };

export const ENV_VARIABLES: Record<keyof PublicEnv, string> = {
  apiUrl: 'EXPO_PUBLIC_API_URL',
};

export class EnvError extends Error {
  override name = 'EnvError';
}

/** Plain HTTP is accepted only for a development machine or the Android emulator. */
const LOCAL_HOST = /^(localhost|127\.0\.0\.1|10\.0\.2\.2|192\.168\.\d{1,3}\.\d{1,3})$/;

function parseUrl(variable: string, value: string): string {
  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new EnvError(`${variable} is not a valid URL.`);
  }
  const secure = url.protocol === 'https:';
  const localHttp = url.protocol === 'http:' && LOCAL_HOST.test(url.hostname);
  if (!secure && !localHttp) {
    throw new EnvError(`${variable} must use https (http is allowed only for a local host).`);
  }
  return value.replace(/\/+$/, '');
}

export function parsePublicEnv(raw: RawPublicEnv): PublicEnv {
  const keys = Object.keys(ENV_VARIABLES) as (keyof PublicEnv)[];
  const missing = keys.filter((key) => !raw[key]?.trim()).map((key) => ENV_VARIABLES[key]);
  if (missing.length > 0) {
    throw new EnvError(`Missing environment variables: ${missing.join(', ')}. See .env.example.`);
  }
  return {
    apiUrl: parseUrl(ENV_VARIABLES.apiUrl, (raw.apiUrl ?? '').trim()),
  };
}

/** Expo inlines EXPO_PUBLIC_ variables only when they are read by their full name. */
export function readPublicEnv(): PublicEnv {
  return parsePublicEnv({
    apiUrl: process.env.EXPO_PUBLIC_API_URL,
  });
}
