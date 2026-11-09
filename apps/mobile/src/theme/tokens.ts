/**
 * Design tokens from the board "Covoiturage — Maquette V1", palette "Nuit + Soleil".
 * Screens and components read colours, sizes and fonts from here, never literals.
 */

export const colors = {
  primary: '#172554',
  primaryPressed: '#0F1A3D',
  accent: '#FFD23F',
  accentPressed: '#F5C518',
  accentBusy: '#F5DE8A',
  ink: '#111827',
  background: '#F5F4F0',
  surface: '#FFFFFF',
  muted: '#5A6070',
  subtle: '#9AA1B0',
  border: '#E6E2D9',
  divider: '#EFECE5',
  segmentTrack: '#EAE6DD',
  segmentInactive: '#474D5C',
  tabBar: '#111827',
  tabInactive: '#B8BECC',
  onPrimary: '#FFFFFF',
  danger: '#B42318',
  dangerSurface: '#FDECEA',
  dangerInk: '#8A1C13',
  success: '#17683B',
  infoSurface: '#E8ECF7',
  disabledSurface: '#E6E2D9',
  notification: '#E5484D',
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
} as const;

export const radii = {
  sm: 12,
  md: 14,
  lg: 16,
  xl: 22,
  xxl: 24,
  sheet: 32,
} as const;

/** Minimum size of anything a finger must hit (WCAG 2.5.5 / Android 48dp guideline). */
export const minTouchTarget = 44;

export const fonts = {
  regular: 'PlusJakartaSans_400Regular',
  medium: 'PlusJakartaSans_500Medium',
  semibold: 'PlusJakartaSans_600SemiBold',
  bold: 'PlusJakartaSans_700Bold',
  extrabold: 'PlusJakartaSans_800ExtraBold',
} as const;

export type TypographyVariant =
  'display' | 'title' | 'heading' | 'body' | 'bodyStrong' | 'label' | 'caption' | 'tiny';

type TypographyStyle = {
  fontFamily: string;
  fontSize: number;
  lineHeight: number;
  letterSpacing?: number;
};

/** Letter spacing on the board is -0.03em for display text and -0.02em for titles. */
export const typography: Record<TypographyVariant, TypographyStyle> = {
  display: { fontFamily: fonts.extrabold, fontSize: 28, lineHeight: 32, letterSpacing: -0.84 },
  title: { fontFamily: fonts.extrabold, fontSize: 22, lineHeight: 28, letterSpacing: -0.44 },
  heading: { fontFamily: fonts.extrabold, fontSize: 17, lineHeight: 22 },
  body: { fontFamily: fonts.regular, fontSize: 16, lineHeight: 22 },
  bodyStrong: { fontFamily: fonts.bold, fontSize: 16, lineHeight: 22 },
  label: { fontFamily: fonts.bold, fontSize: 14, lineHeight: 18 },
  caption: { fontFamily: fonts.semibold, fontSize: 13, lineHeight: 18 },
  tiny: { fontFamily: fonts.bold, fontSize: 11, lineHeight: 14 },
};

/** Avatar backgrounds and initials colours (rounded squares on the board). */
export const avatarPalette = [
  { background: '#FDE6C8', foreground: '#7A3A00' },
  { background: '#DCE8FB', foreground: '#1E4B91' },
  { background: '#DDF3E4', foreground: '#17683B' },
  { background: '#F3E1F7', foreground: '#6B2A7E' },
  { background: '#FFF1C2', foreground: '#6B4E00' },
] as const;

/** Floating tab bar geometry, shared by the bar and by screens that must clear it. */
export const tabBar = {
  height: 72,
  sideInset: 12,
  bottomInset: 16,
  radius: 24,
  publishSize: 50,
} as const;

/** Vertical space a screen keeps free at the bottom so content never hides under the tab bar. */
export function tabBarClearance(safeAreaBottom: number): number {
  return tabBar.height + tabBar.bottomInset + safeAreaBottom + spacing.lg;
}
