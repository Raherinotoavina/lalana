import { AppText, Card, Screen } from '@/ui';
import { colors } from '@/theme/tokens';

export type ComingSoonScreenProps = {
  title: string;
  description: string;
};

/** Placeholder for a tab whose feature arrives in a later phase. */
export function ComingSoonScreen({ title, description }: ComingSoonScreenProps) {
  return (
    <Screen withTabBar>
      <AppText variant="display" accessibilityRole="header">
        {title}
      </AppText>
      <Card>
        <AppText variant="bodyStrong">Bientôt disponible</AppText>
        <AppText color={colors.muted}>{description}</AppText>
      </Card>
    </Screen>
  );
}
