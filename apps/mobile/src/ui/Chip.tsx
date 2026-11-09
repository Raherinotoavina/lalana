import { Pressable, StyleSheet } from 'react-native';

import { colors, radii, spacing } from '@/theme/tokens';

import { AppText } from './AppText';

export type ChipProps = {
  label: string;
  selected: boolean;
  onPress: () => void;
  testID?: string;
};

/** One choice in a group (time flexibility, vehicle type…). Wrap a set in a radiogroup. */
export function Chip({ label, selected, onPress, testID }: ChipProps) {
  return (
    <Pressable
      testID={testID}
      accessibilityRole="radio"
      accessibilityLabel={label}
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      hitSlop={{ top: 2, bottom: 2 }}
      style={[styles.chip, selected ? styles.selected : styles.idle]}
    >
      <AppText variant="label" color={selected ? colors.onPrimary : colors.ink}>
        {label}
      </AppText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  chip: {
    height: 40,
    paddingHorizontal: spacing.md,
    borderRadius: radii.sm,
    alignItems: 'center',
    justifyContent: 'center',
  },
  selected: { backgroundColor: colors.primary },
  idle: { backgroundColor: colors.background },
});
