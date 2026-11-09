import { focusManager } from '@tanstack/react-query';
import { AppState, Platform } from 'react-native';

import {
  MAX_QUERY_RETRIES,
  createQueryClient,
  handleAppStateChange,
  shouldRetry,
  statusOf,
  subscribeToAppFocus,
} from '../queryClient';

describe('statusOf', () => {
  it.each([
    [{ status: 404 }, 404],
    [{ status: '404' }, undefined],
    [new Error('network'), undefined],
    [null, undefined],
    ['boom', undefined],
  ])('%p → %p', (error, status) => {
    expect(statusOf(error)).toBe(status);
  });
});

describe('shouldRetry', () => {
  it('retries network and server errors up to the limit', () => {
    expect(shouldRetry(0, new Error('Network request failed'))).toBe(true);
    expect(shouldRetry(1, { status: 503 })).toBe(true);
    expect(shouldRetry(MAX_QUERY_RETRIES, { status: 503 })).toBe(false);
  });

  it('never retries client errors except timeouts and rate limits', () => {
    expect(shouldRetry(0, { status: 400 })).toBe(false);
    expect(shouldRetry(0, { status: 401 })).toBe(false);
    expect(shouldRetry(0, { status: 404 })).toBe(false);
    expect(shouldRetry(0, { status: 408 })).toBe(true);
    expect(shouldRetry(0, { status: 429 })).toBe(true);
  });
});

describe('createQueryClient', () => {
  it('caches for 30 s, retries queries with shouldRetry and never retries mutations', () => {
    const client = createQueryClient();
    const defaults = client.getDefaultOptions();
    expect(defaults.queries?.staleTime).toBe(30_000);
    expect(defaults.queries?.retry).toBe(shouldRetry);
    expect(defaults.mutations?.retry).toBe(false);
  });
});

describe('app focus', () => {
  afterEach(() => {
    focusManager.setFocused(undefined);
    jest.restoreAllMocks();
  });

  it('follows the app state on native', () => {
    handleAppStateChange('background');
    expect(focusManager.isFocused()).toBe(false);
    handleAppStateChange('active');
    expect(focusManager.isFocused()).toBe(true);
  });

  it('leaves focus to the browser on web', () => {
    const original = Platform.OS;
    Object.defineProperty(Platform, 'OS', { value: 'web', configurable: true });
    const spy = jest.spyOn(focusManager, 'setFocused');
    handleAppStateChange('background');
    expect(spy).not.toHaveBeenCalled();
    Object.defineProperty(Platform, 'OS', { value: original, configurable: true });
  });

  it('subscribes to app state changes and unsubscribes', () => {
    const remove = jest.fn();
    const add = jest
      .spyOn(AppState, 'addEventListener')
      .mockReturnValue({ remove } as unknown as ReturnType<typeof AppState.addEventListener>);
    const unsubscribe = subscribeToAppFocus();
    expect(add).toHaveBeenCalledWith('change', handleAppStateChange);
    unsubscribe();
    expect(remove).toHaveBeenCalledTimes(1);
  });
});
