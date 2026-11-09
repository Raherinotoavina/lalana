import { EnvError, parsePublicEnv, readPublicEnv, type RawPublicEnv } from '../env';

const VALID: RawPublicEnv = {
  apiUrl: 'https://api.staging.lalana.example/',
  supabaseUrl: 'https://abc.supabase.co',
  supabaseAnonKey: ' public-anon-key ',
};

describe('parsePublicEnv', () => {
  it('trims values and drops trailing slashes', () => {
    expect(parsePublicEnv(VALID)).toEqual({
      apiUrl: 'https://api.staging.lalana.example',
      supabaseUrl: 'https://abc.supabase.co',
      supabaseAnonKey: 'public-anon-key',
    });
  });

  it('names every missing variable', () => {
    expect(() => parsePublicEnv({ ...VALID, apiUrl: undefined, supabaseAnonKey: '  ' })).toThrow(
      'Missing environment variables: EXPO_PUBLIC_API_URL, EXPO_PUBLIC_SUPABASE_ANON_KEY',
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
    process.env.EXPO_PUBLIC_SUPABASE_URL = 'https://abc.supabase.co';
    process.env.EXPO_PUBLIC_SUPABASE_ANON_KEY = 'anon';
    expect(readPublicEnv().apiUrl).toBe('http://localhost:5080');
  });
});
