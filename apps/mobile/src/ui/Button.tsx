import type { ReactNode } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { colors, minTouchTarget, radii, spacing } from '@/theme/tokens';

import { AppText } from './AppText';

export type ButtonVariant = 'accent' | 'ghost';

export type ButtonProps = {
  label: string;
  onPress: () => void;
  /** "accent" is the single yellow call to action of a screen; "ghost" is everything else. */
  variant?: ButtonVariant;
  disabled?: boolean;
  /** Shows a spinner and `loadingLabel`, and ignores presses. */
  loading?: boolean;
  loadingLabel?: string;
  icon?: ReactNode;
  accessibilityHint?: string;
  testID?: string;
};

export function Button({
  label,
  onPress,
  variant = 'accent',
  disabled = false,
  loading = false,
  loadingLabel,
  icon,
  accessibilityHint,
  testID,
}: ButtonProps) {
  const inactive = disabled || loading;
  const shownLabel = loading && loadingLabel ? loadingLabel : label;
  const textColor = disabled ? colors.muted : colors.ink;

  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={shownLabel}
      accessibilityHint={accessibilityHint}
      accessibilityState={{ disabled: inactive, busy: loading }}
      disabled={inactive}
      onPress={onPress}
      style={({ pressed }) => [
        styles.base,
        variant === 'accent' ? styles.accent : styles.ghost,
        pressed && (variant === 'accent' ? styles.accentPressed : styles.ghostPressed),
        loading && variant === 'accent' && styles.accentBusy,
        disabled && styles.disabled,
      ]}
    >
      <View style={styles.content}>
        {loading ? <ActivityIndicator testID="button-spinner" color={colors.ink} /> : icon}
        <AppText
          variant={variant === 'accent' ? 'heading' : 'bodyStrong'}
          color={textColor}
          numberOfLines={1}
        >
          {shownLabel}
        </AppText>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    minHeight: 56,
    minWidth: minTouchTarget,
    borderRadius: radii.lg,
    paddingHorizontal: spacing.lg,
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  accent: { backgroundColor: colors.accent },
  accentPressed: { backgroundColor: colors.accentPressed },
  accentBusy: { backgroundColor: colors.accentBusy },
  ghost: { backgroundColor: colors.surface, borderWidth: 1.5, borderColor: colors.border },
  ghostPressed: { backgroundColor: colors.background },
  disabled: { backgroundColor: colors.disabledSurface, borderColor: colors.disabledSurface },
});
