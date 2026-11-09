import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Card } from '../Card';

describe('Card', () => {
  it('is a plain container without onPress', async () => {
    await render(
      <Card testID="card">
        <Text>Ivato → Analakely</Text>
      </Card>,
    );
    expect(screen.getByText('Ivato → Analakely')).toBeOnTheScreen();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('becomes one button with onPress', async () => {
    const onPress = jest.fn();
    await render(
      <Card onPress={onPress} accessibilityLabel="Trajet de 07:00">
        <Text>07:00</Text>
      </Card>,
    );
    await fireEvent.press(screen.getByRole('button', { name: 'Trajet de 07:00' }));
    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
