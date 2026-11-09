import { House, MessageCircle, Plus, Ticket, User, type Icon } from '@/ui/icons';

export type TabDefinition = {
  name: 'index' | 'bookings' | 'publish' | 'messages' | 'profile';
  href: '/' | '/bookings' | '/publish' | '/messages' | '/profile';
  label: string;
  icon: Icon;
  variant: 'default' | 'publish';
};

/** The five tabs of the board, left to right. */
export const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Accueil', icon: House, variant: 'default' },
  { name: 'bookings', href: '/bookings', label: 'Réservations', icon: Ticket, variant: 'default' },
  { name: 'publish', href: '/publish', label: 'Publier', icon: Plus, variant: 'publish' },
  {
    name: 'messages',
    href: '/messages',
    label: 'Messages',
    icon: MessageCircle,
    variant: 'default',
  },
  { name: 'profile', href: '/profile', label: 'Profil', icon: User, variant: 'default' },
];
