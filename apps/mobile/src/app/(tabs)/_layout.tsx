import { TabList, TabSlot, TabTrigger, Tabs } from 'expo-router/ui';
import { StyleSheet } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { TABS } from '@/features/shell/tabs';
import { colors, tabBar } from '@/theme/tokens';
import { TabBarButton } from '@/ui';

export default function TabsLayout() {
  const insets = useSafeAreaInsets();
  return (
    <Tabs style={styles.root}>
      <TabSlot />
      <TabList
        accessibilityRole="tablist"
        aria-label="Navigation principale"
        style={[styles.bar, { bottom: tabBar.bottomInset + insets.bottom }]}
      >
        {TABS.map((tab) => (
          <TabTrigger key={tab.name} name={tab.name} href={tab.href} asChild>
            <TabBarButton label={tab.label} icon={tab.icon} variant={tab.variant} />
          </TabTrigger>
        ))}
      </TabList>
    </Tabs>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  bar: {
    position: 'absolute',
    left: tabBar.sideInset,
    right: tabBar.sideInset,
    height: tabBar.height,
    borderRadius: tabBar.radius,
    backgroundColor: colors.tabBar,
    flexDirection: 'row',
    justifyContent: 'space-around',
    alignItems: 'center',
    paddingHorizontal: 6,
  },
});
