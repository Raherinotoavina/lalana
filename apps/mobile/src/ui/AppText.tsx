import { StyleSheet, Text, type TextProps } from 'react-native';

import { colors, typography, type TypographyVariant } from '@/theme/tokens';

export type AppTextProps = TextProps & {
  variant?: TypographyVariant;
  color?: string;
};

/** Text in the app font. Use a variant instead of setting font sizes by hand. */
export function AppText({ variant = 'body', color = colors.ink, style, ...rest }: AppTextProps) {
  return <Text {...rest} style={[styles.base, typography[variant], { color }, style]} />;
}

const styles = StyleSheet.create({
  base: { includeFontPadding: false },
});
