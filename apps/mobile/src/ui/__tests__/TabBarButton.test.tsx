import { fireEvent, render, screen } from '@testing-library/react-native';

import { House, Plus } from '../icons';
import { TabBarButton } from '../TabBarButton';

describe('TabBarButton', () => {
  it('shows its label and reports selection', async () => {
    const onPress = jest.fn();
    await render(<TabBarButton label="Accueil" icon={House} isFocused onPress={onPress} />);
    const tab = screen.getByRole('tab', { name: 'Accueil' });
    expect(tab).toBeSelected();
    expect(screen.getByText('Accueil')).toBeOnTheScreen();
    await fireEvent.press(tab);
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('renders the publish button without a visible label', async () => {
    await render(<TabBarButton label="Publier" icon={Plus} variant="publish" />);
    expect(screen.getByRole('tab', { name: 'Publier' })).not.toBeSelected();
    expect(screen.queryByText('Publier')).toBeNull();
  });

  it('marks the focused publish button', async () => {
    await render(<TabBarButton label="Publier" icon={Plus} variant="publish" isFocused />);
    expect(screen.getByRole('tab', { name: 'Publier' })).toBeSelected();
  });
});
