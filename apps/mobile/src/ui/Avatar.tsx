import { StyleSheet, View } from 'react-native';

import { avatarPalette, fonts } from '@/theme/tokens';

import { AppText } from './AppText';

export type AvatarProps = {
  /** Full display name, for example "Hery Rakoto". */
  name: string;
  size?: number;
  /** Most avatars sit next to the person's name; only announce them when they stand alone. */
  decorative?: boolean;
};

export function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : '';
  return (first + last).toUpperCase();
}

/** Same name, same colour, on every screen. */
export function paletteIndexFor(name: string): number {
  let hash = 0;
  for (const char of name) hash = (hash * 31 + char.charCodeAt(0)) | 0;
  return Math.abs(hash) % avatarPalette.length;
}

/** Rounded-square avatar with initials (photos come with Phase 04). */
export function Avatar({ name, size = 36, decorative = true }: AvatarProps) {
  const palette = avatarPalette[paletteIndexFor(name)] ?? avatarPalette[0];
  return (
    <View
      accessible={!decorative}
      accessibilityRole={decorative ? undefined : 'image'}
      accessibilityLabel={decorative ? undefined : name}
      importantForAccessibility={decorative ? 'no-hide-descendants' : 'yes'}
      style={[
        styles.avatar,
        {
          width: size,
          height: size,
          borderRadius: Math.round(size * 0.39),
          backgroundColor: palette.background,
        },
      ]}
    >
      <AppText
        color={palette.foreground}
        style={{ fontFamily: fonts.extrabold, fontSize: Math.round(size * 0.36) }}
      >
        {initialsOf(name)}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  avatar: { alignItems: 'center', justifyContent: 'center' },
});
