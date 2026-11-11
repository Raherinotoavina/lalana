import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react-native';
import type { ReactNode } from 'react';

import * as account from '@/services/account';
import type { Me } from '@/services/account';

import { meQueryKey, useMe } from '../useMe';

function wrapper(queryClient: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

const me: Me = {
  id: 'a1b2',
  firstName: 'Sitraka',
  lastName: 'Rabe',
  phone: '+261341234567',
  phoneConfirmed: true,
  email: null,
  emailConfirmed: false,
  hasProfile: false,
};

describe('useMe', () => {
  afterEach(() => jest.restoreAllMocks());

  it('loads the account through the account service and caches it', async () => {
    jest.spyOn(account, 'fetchMe').mockResolvedValue(me);
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    const { result } = await renderHook(() => useMe(), { wrapper: wrapper(queryClient) });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toEqual(me);
    expect(queryClient.getQueryData(meQueryKey)).toEqual(me);
    queryClient.clear();
  });
});
