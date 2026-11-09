import { fireEvent, render, screen } from '@testing-library/react-native';

import { Chip } from '../Chip';

describe('Chip', () => {
  it('reports its checked state', async () => {
    await render(<Chip label="± 30 min" selected onPress={jest.fn()} />);
    expect(screen.getByRole('radio', { name: '± 30 min' })).toBeChecked();
  });

  it('calls onPress when an unselected chip is pressed', async () => {
    const onPress = jest.fn();
    await render(<Chip label="± 1 h" selected={false} onPress={onPress} />);
    const chip = screen.getByRole('radio', { name: '± 1 h' });
    expect(chip).not.toBeChecked();
    await fireEvent.press(chip);
    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
