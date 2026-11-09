import { render, screen } from '@testing-library/react-native';

import { Logo } from '../Logo';

describe('Logo', () => {
  it('is one image named lalana, with the wordmark', async () => {
    await render(<Logo />);
    expect(screen.getByRole('image', { name: 'lalana' })).toBeOnTheScreen();
    expect(screen.getByText('lalana')).toBeOnTheScreen();
  });

  it('can show the mark alone at a larger size', async () => {
    await render(<Logo withWordmark={false} size={52} />);
    expect(screen.getByRole('image', { name: 'lalana' })).toBeOnTheScreen();
    expect(screen.queryByText('lalana')).toBeNull();
  });
});
