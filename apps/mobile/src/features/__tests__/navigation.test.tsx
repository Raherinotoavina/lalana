import { fireEvent, renderRouter, screen } from 'expo-router/testing-library';

import TabsLayout from '@/app/(tabs)/_layout';
import BookingsRoute from '@/app/(tabs)/bookings';
import HomeRoute from '@/app/(tabs)/index';
import MessagesRoute from '@/app/(tabs)/messages';
import ProfileRoute from '@/app/(tabs)/profile';
import PublishRoute from '@/app/(tabs)/publish';

const ROUTES = {
  '(tabs)/_layout': TabsLayout,
  '(tabs)/index': HomeRoute,
  '(tabs)/bookings': BookingsRoute,
  '(tabs)/publish': PublishRoute,
  '(tabs)/messages': MessagesRoute,
  '(tabs)/profile': ProfileRoute,
};

describe('tab bar shell', () => {
  it('opens on Accueil with the five tabs of the board', async () => {
    await renderRouter(ROUTES, { initialUrl: '/' });

    expect(screen).toHavePathname('/');
    expect(screen.getByText('Où allez-vous ?')).toBeOnTheScreen();
    const names = screen.getAllByRole('tab').map((tab) => tab.props.accessibilityLabel);
    expect(names).toEqual(['Accueil', 'Réservations', 'Publier', 'Messages', 'Profil']);
    expect(screen.getByRole('tab', { name: 'Accueil' })).toBeSelected();
  });

  it.each([
    ['Réservations', '/bookings', 'Mes réservations'],
    ['Publier', '/publish', 'Proposez des places sur votre trajet ou publiez votre demande.'],
    ['Messages', '/messages', 'Échangez avec les conducteurs et les passagers de vos trajets.'],
    ['Profil', '/profile', 'Votre profil, vos véhicules et vos avis apparaîtront ici.'],
  ])('switches to %s', async (tab, pathname, text) => {
    await renderRouter(ROUTES, { initialUrl: '/' });

    await fireEvent.press(screen.getByRole('tab', { name: tab }));

    expect(screen).toHavePathname(pathname);
    expect(await screen.findByText(text)).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: tab })).toBeSelected();
    expect(screen.getByRole('tab', { name: 'Accueil' })).not.toBeSelected();
  });
});
