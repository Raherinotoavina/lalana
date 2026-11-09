import { useFonts } from '@expo-google-fonts/plus-jakarta-sans';
import { renderRouter, screen } from 'expo-router/testing-library';
import * as SplashScreen from 'expo-splash-screen';
import { Text } from 'react-native';

import RootLayout from '@/app/_layout';

jest.mock('@expo-google-fonts/plus-jakarta-sans', () => ({
  ...jest.requireActual('@expo-google-fonts/plus-jakarta-sans'),
  useFonts: jest.fn(),
}));
jest.mock('expo-splash-screen', () => ({
  preventAutoHideAsync: jest.fn(() => Promise.resolve(true)),
  hideAsync: jest.fn(() => Promise.resolve(true)),
}));

const mockedUseFonts = jest.mocked(useFonts);
const Home = () => <Text>Accueil prêt</Text>;

describe('root layout', () => {
  beforeEach(() => jest.mocked(SplashScreen.hideAsync).mockClear());

  it('keeps the splash screen while fonts load', async () => {
    mockedUseFonts.mockReturnValue([false, null]);
    await renderRouter({ _layout: RootLayout, index: Home }, { initialUrl: '/' });
    expect(screen.queryByText('Accueil prêt')).toBeNull();
    expect(SplashScreen.hideAsync).not.toHaveBeenCalled();
  });

  it('renders the app and hides the splash once fonts are loaded', async () => {
    mockedUseFonts.mockReturnValue([true, null]);
    await renderRouter({ _layout: RootLayout, index: Home }, { initialUrl: '/' });
    expect(screen.getByText('Accueil prêt')).toBeOnTheScreen();
    expect(SplashScreen.hideAsync).toHaveBeenCalled();
  });

  it('falls back to the system font when fonts fail', async () => {
    mockedUseFonts.mockReturnValue([false, new Error('offline')]);
    await renderRouter({ _layout: RootLayout, index: Home }, { initialUrl: '/' });
    expect(screen.getByText('Accueil prêt')).toBeOnTheScreen();
  });
});
