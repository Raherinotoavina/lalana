import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { colors } from '@/theme/tokens';

import { Button } from '../Button';

describe('Button', () => {
  it('calls onPress when pressed', async () => {
    const onPress = jest.fn();
    await render(<Button label="Se connecter" onPress={onPress} />);
    await fireEvent.press(screen.getByRole('button', { name: 'Se connecter' }));
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('ignores presses and reports disabled when disabled', async () => {
    const onPress = jest.fn();
    await render(<Button label="Vérifier" onPress={onPress} disabled />);
    const button = screen.getByRole('button', { name: 'Vérifier' });
    await fireEvent.press(button);
    expect(onPress).not.toHaveBeenCalled();
    expect(button).toBeDisabled();
  });

  it('shows a spinner and the loading label while loading', async () => {
    const onPress = jest.fn();
    await render(
      <Button label="Se connecter" loadingLabel="Connexion…" onPress={onPress} loading />,
    );
    const button = screen.getByRole('button', { name: 'Connexion…' });
    expect(screen.getByTestId('button-spinner')).toBeOnTheScreen();
    expect(button).toBeBusy();
    await fireEvent.press(button);
    expect(onPress).not.toHaveBeenCalled();
  });

  it('keeps the label while loading when no loading label is given', async () => {
    await render(<Button label="Publier" onPress={jest.fn()} loading variant="ghost" />);
    expect(screen.getByRole('button', { name: 'Publier' })).toBeBusy();
  });

  it('renders the ghost variant with an icon', async () => {
    await render(
      <Button label="Google" variant="ghost" icon={<Text>G</Text>} onPress={jest.fn()} />,
    );
    expect(screen.getByText('G')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Google' })).toBeEnabled();
  });

  it('uses white text on the primary action and ink on the ghost button', async () => {
    await render(
      <>
        <Button label="Se connecter" onPress={jest.fn()} />
        <Button label="Continuer avec Google" variant="ghost" onPress={jest.fn()} />
        <Button label="Vérifier" onPress={jest.fn()} disabled />
      </>,
    );
    expect(screen.getByText('Se connecter')).toHaveStyle({ color: colors.onAccent });
    expect(screen.getByText('Continuer avec Google')).toHaveStyle({ color: colors.ink });
    expect(screen.getByText('Vérifier')).toHaveStyle({ color: colors.muted });
  });
});
