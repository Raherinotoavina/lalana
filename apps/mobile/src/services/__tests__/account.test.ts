import { client } from '../api/client.gen';
import { fetchMe } from '../account';
import { ApiError, configureApiClient, setAccessTokenProvider } from '../apiClient';

const API_URL = 'http://localhost:5080';

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': status < 400 ? 'application/json' : 'application/problem+json' },
  });
}

describe('account service', () => {
  const fetchMock = jest.fn<Promise<Response>, [Request]>();

  beforeEach(() => {
    fetchMock.mockReset();
    configureApiClient({ apiUrl: API_URL });
    client.setConfig({ fetch: fetchMock as unknown as typeof fetch });
    setAccessTokenProvider(() => 'access-token');
  });

  it('calls GET /me on the configured API with the bearer token', async () => {
    fetchMock.mockResolvedValue(jsonResponse(200, { id: 'a1b2' }));

    await expect(fetchMe()).resolves.toEqual({ id: 'a1b2' });

    const request = fetchMock.mock.calls[0]?.[0];
    expect(request?.url).toBe(`${API_URL}/me`);
    expect(request?.method).toBe('GET');
    expect(request?.headers.get('Authorization')).toBe('Bearer access-token');
  });

  it('sends no Authorization header when signed out', async () => {
    setAccessTokenProvider(() => undefined);
    fetchMock.mockResolvedValue(jsonResponse(200, { id: 'a1b2' }));

    await fetchMe();

    expect(fetchMock.mock.calls[0]?.[0].headers.get('Authorization')).toBeNull();
  });

  it('throws an ApiError carrying the status and the problem details', async () => {
    fetchMock.mockResolvedValue(jsonResponse(401, { title: 'Unauthorized', status: 401 }));

    const error = await fetchMe().catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 401, problem: { status: 401 } });
  });
});

describe('account service without network', () => {
  it('rejects when the request cannot be sent', async () => {
    configureApiClient({ apiUrl: API_URL });
    client.setConfig({
      fetch: jest.fn().mockRejectedValue(new TypeError('Network request failed')),
    });

    await expect(fetchMe()).rejects.toBeDefined();
  });
});

describe('configureApiClient', () => {
  const saved = { ...process.env };
  afterEach(() => {
    process.env = { ...saved };
  });

  it('reads the API URL from the environment by default', () => {
    process.env.EXPO_PUBLIC_API_URL = 'https://api.lalana.example/';
    configureApiClient();
    expect(client.getConfig().baseUrl).toBe('https://api.lalana.example');
  });
});
