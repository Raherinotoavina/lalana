import { useQuery } from '@tanstack/react-query';

import { fetchMe } from '@/services/account';

export const meQueryKey = ['me'] as const;

/** The signed-in member's account, cached by TanStack Query. */
export function useMe() {
  return useQuery({ queryKey: meQueryKey, queryFn: fetchMe });
}
