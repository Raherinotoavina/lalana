import { fireEvent, render, screen } from '@testing-library/react-native';

import { TextField } from '../TextField';

describe('TextField', () => {
  it('is labelled and forwards typing', async () => {
    const onChangeText = jest.fn();
    await render(<TextField label="Téléphone ou email" onChangeText={onChangeText} />);
    await fireEvent.changeText(screen.getByLabelText('Téléphone ou email'), '034 12 345 67');
    expect(onChangeText).toHaveBeenCalledWith('034 12 345 67');
  });

  it('shows the hint when there is no error', async () => {
    await render(<TextField label="Email" hint="Facultatif" />);
    expect(screen.getByText('Facultatif')).toBeOnTheScreen();
  });

  it('shows the error instead of the hint and marks the field invalid', async () => {
    await render(<TextField label="Email" hint="Facultatif" error="Cet email a déjà un compte." />);
    expect(screen.getByText('Cet email a déjà un compte.')).toBeOnTheScreen();
    expect(screen.queryByText('Facultatif')).toBeNull();
    expect(screen.getByLabelText('Email').props['aria-invalid']).toBe(true);
  });

  it('hides the password until the eye button is pressed', async () => {
    await render(<TextField label="Mot de passe" secure />);
    const input = screen.getByLabelText('Mot de passe');
    expect(input.props.secureTextEntry).toBe(true);

    await fireEvent.press(screen.getByRole('button', { name: 'Afficher le mot de passe' }));
    expect(screen.getByLabelText('Mot de passe').props.secureTextEntry).toBe(false);

    await fireEvent.press(screen.getByRole('button', { name: 'Masquer le mot de passe' }));
    expect(screen.getByLabelText('Mot de passe').props.secureTextEntry).toBe(true);
  });

  it('has no eye button on a plain field', async () => {
    await render(<TextField label="Prénom" />);
    expect(screen.queryByRole('button')).toBeNull();
  });
});
