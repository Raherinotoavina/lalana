import { StyleSheet, View } from 'react-native';
import Svg, { Circle, Path } from 'react-native-svg';

import { colors, radii, spacing } from '@/theme/tokens';

import { AppText } from './AppText';

export type LogoProps = {
  /** Show the "lalana" wordmark next to the mark. */
  withWordmark?: boolean;
  size?: number;
};

/** The lalana mark: a winding road between two points, on a mango rounded square. */
export function Logo({ withWordmark = true, size = 38 }: LogoProps) {
  const glyph = Math.round(size * 0.58);
  return (
    <View style={styles.row} accessible accessibilityRole="image" accessibilityLabel="lalana">
      <View
        style={[
          styles.mark,
          { width: size, height: size, borderRadius: size >= 44 ? radii.md : radii.sm },
        ]}
      >
        <Svg
          width={glyph}
          height={glyph}
          viewBox="0 0 24 24"
          fill="none"
          stroke={colors.onAccent}
          strokeWidth={2.4}
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <Path d="M6 19c0-7 12-5 12-13" />
          <Circle cx={6} cy={19} r={1.6} />
          <Circle cx={18} cy={5} r={1.6} />
        </Svg>
      </View>
      {withWordmark ? (
        <AppText variant="display" style={styles.wordmark} importantForAccessibility="no">
          lalana
        </AppText>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm + 2 },
  mark: { backgroundColor: colors.accent, alignItems: 'center', justifyContent: 'center' },
  wordmark: { fontSize: 23, lineHeight: 28, letterSpacing: -0.69 },
});
