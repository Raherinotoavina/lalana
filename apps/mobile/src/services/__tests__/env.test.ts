import { EnvError, parsePublicEnv, readPublicEnv, type RawPublicEnv } from '../env';

const VALID: RawPublicEnv = {
  apiUrl: ' https://api.staging.lalana.example/ ',
};

describe('parsePublicEnv', () => {
  it('trims values and drops trailing slashes', () => {
    expect(parsePublicEnv(VALID)).toEqual({ apiUrl: 'https://api.staging.lalana.example' });
  });

  it('names the missing variable', () => {
    expect(() => parsePublicEnv({ apiUrl: '  ' })).toThrow(
      'Missing environment variables: EXPO_PUBLIC_API_URL',
    );
  });

  it('rejects a value that is not a URL', () => {
    expect(() => parsePublicEnv({ ...VALID, apiUrl: 'api.lalana' })).toThrow(EnvError);
  });

  it('rejects plain http on a public host', () => {
    expect(() => parsePublicEnv({ ...VALID, apiUrl: 'http://api.lalana.example' })).toThrow(
      'EXPO_PUBLIC_API_URL must use https',
    );
  });

  it.each(['http://localhost:5080', 'http://10.0.2.2:5080', 'http://192.168.1.20:5080'])(
    'accepts plain http for the local host %p',
    (apiUrl) => {
      expect(parsePublicEnv({ ...VALID, apiUrl }).apiUrl).toBe(apiUrl);
    },
  );
});

describe('readPublicEnv', () => {
  const saved = { ...process.env };
  afterEach(() => {
    process.env = { ...saved };
  });

  it('reads the EXPO_PUBLIC_ variables', () => {
    process.env.EXPO_PUBLIC_API_URL = 'http://localhost:5080';
    expect(readPublicEnv().apiUrl).toBe('http://localhost:5080');
  });
});
