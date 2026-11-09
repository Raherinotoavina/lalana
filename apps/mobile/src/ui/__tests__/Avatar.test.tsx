import { render, screen } from '@testing-library/react-native';

import { Avatar, initialsOf, paletteIndexFor } from '../Avatar';
import { avatarPalette } from '@/theme/tokens';

describe('initialsOf', () => {
  it.each([
    ['Hery Rakoto', 'HR'],
    ['  fanja   rabe  ', 'FR'],
    ['Sitraka', 'S'],
    ['Jean Paul Rabe', 'JR'],
    ['', '?'],
  ])('%p → %p', (name, initials) => {
    expect(initialsOf(name)).toBe(initials);
  });
});

describe('paletteIndexFor', () => {
  it('is stable and within the palette', async () => {
    const index = paletteIndexFor('Hery Rakoto');
    expect(paletteIndexFor('Hery Rakoto')).toBe(index);
    expect(index).toBeGreaterThanOrEqual(0);
    expect(index).toBeLessThan(avatarPalette.length);
  });
});

describe('Avatar', () => {
  it('is hidden from screen readers by default', async () => {
    await render(<Avatar name="Hery Rakoto" />);
    expect(screen.getByText('HR', { includeHiddenElements: true })).toBeOnTheScreen();
    expect(screen.queryByText('HR')).toBeNull();
    expect(screen.queryByRole('image')).toBeNull();
  });

  it('is announced by name when not decorative', async () => {
    await render(<Avatar name="Fanja Rabe" size={56} decorative={false} />);
    expect(screen.getByRole('image', { name: 'Fanja Rabe' })).toBeOnTheScreen();
  });
});
