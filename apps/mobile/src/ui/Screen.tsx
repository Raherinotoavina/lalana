import type { ReactNode } from 'react';
import { ScrollView, StyleSheet } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { colors, spacing, tabBarClearance } from '@/theme/tokens';

export type ScreenProps = {
  children: ReactNode;
  /** Screens inside the tab navigator keep room for the floating tab bar. */
  withTabBar?: boolean;
  testID?: string;
};

export function Screen({ children, withTabBar = false, testID }: ScreenProps) {
  const insets = useSafeAreaInsets();
  return (
    <ScrollView
      testID={testID}
      style={styles.root}
      contentContainerStyle={[
        styles.content,
        {
          paddingTop: insets.top + spacing.lg,
          paddingBottom: withTabBar ? tabBarClearance(insets.bottom) : insets.bottom + spacing.xl,
        },
      ]}
      keyboardShouldPersistTaps="handled"
    >
      {children}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  content: { paddingHorizontal: spacing.lg, gap: spacing.lg + 2 },
});
