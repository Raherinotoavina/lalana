import type { TabTriggerSlotProps } from 'expo-router/ui';
import type { ComponentRef, Ref } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { colors, minTouchTarget, radii, tabBar } from '@/theme/tokens';

import { AppText } from './AppText';
import { iconDefaults, type Icon } from './icons';

export type TabBarButtonProps = Omit<TabTriggerSlotProps, 'ref'> & {
  ref?: Ref<ComponentRef<typeof Pressable>>;
  label: string;
  icon: Icon;
  /** The centre "+" button: accent square, no visible label. */
  variant?: 'default' | 'publish';
};

/** One button of the floating tab bar, rendered by expo-router's TabTrigger (asChild). */
export function TabBarButton({
  label,
  icon: IconComponent,
  variant = 'default',
  isFocused = false,
  ref,
  ...pressableProps
}: TabBarButtonProps) {
  const color = isFocused ? colors.accent : colors.tabInactive;
  return (
    <Pressable
      {...pressableProps}
      ref={ref}
      accessibilityRole="tab"
      accessibilityLabel={label}
      accessibilityState={{ selected: isFocused }}
      style={styles.tab}
    >
      {variant === 'publish' ? (
        <View style={[styles.publish, isFocused && styles.publishFocused]}>
          <IconComponent color={colors.onAccent} size={iconDefaults.size} strokeWidth={2.4} />
        </View>
      ) : (
        <>
          <IconComponent color={color} {...iconDefaults} />
          <AppText variant="tiny" color={color}>
            {label}
          </AppText>
        </>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  tab: {
    minWidth: 56,
    minHeight: 52,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
  },
  publish: {
    width: tabBar.publishSize,
    height: tabBar.publishSize,
    minWidth: minTouchTarget,
    borderRadius: radii.lg,
    backgroundColor: colors.accent,
    alignItems: 'center',
    justifyContent: 'center',
  },
  publishFocused: { backgroundColor: colors.accentPressed },
});
