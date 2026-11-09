import { View, StyleSheet } from 'react-native';

import { colors, spacing } from '@/theme/tokens';
import { AppText, Card, Logo, Screen } from '@/ui';

/** Accueil shell. Search and the trip feed arrive with Phase 06. */
export function HomeScreen() {
  return (
    <Screen withTabBar>
      <Logo />
      <View style={styles.greeting}>
        <AppText variant="display" accessibilityRole="header">
          Où allez-vous ?
        </AppText>
      </View>
      <Card>
        <AppText variant="bodyStrong">Les trajets arrivent bientôt</AppText>
        <AppText color={colors.muted}>
          Vous pourrez chercher un trajet et voir les départs proches ici.
        </AppText>
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  greeting: { gap: spacing.xs },
});
