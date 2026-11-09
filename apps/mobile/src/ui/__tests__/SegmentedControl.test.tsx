import { fireEvent, render, screen } from '@testing-library/react-native';

import { SegmentedControl } from '../SegmentedControl';

const OPTIONS = [
  { value: 'drivers', label: 'Conducteurs' },
  { value: 'passengers', label: 'Passagers' },
] as const;

describe('SegmentedControl', () => {
  it('marks the current option selected', async () => {
    await render(
      <SegmentedControl
        accessibilityLabel="Fil"
        options={OPTIONS}
        value="drivers"
        onChange={jest.fn()}
      />,
    );
    expect(screen.getByRole('tab', { name: 'Conducteurs' })).toBeSelected();
    expect(screen.getByRole('tab', { name: 'Passagers' })).not.toBeSelected();
  });

  it('reports a new choice and ignores the current one', async () => {
    const onChange = jest.fn();
    await render(
      <SegmentedControl
        accessibilityLabel="Fil"
        options={OPTIONS}
        value="drivers"
        onChange={onChange}
      />,
    );
    await fireEvent.press(screen.getByRole('tab', { name: 'Conducteurs' }));
    expect(onChange).not.toHaveBeenCalled();
    await fireEvent.press(screen.getByRole('tab', { name: 'Passagers' }));
    expect(onChange).toHaveBeenCalledWith('passengers');
  });
});
