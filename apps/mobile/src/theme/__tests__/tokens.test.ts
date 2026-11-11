import { colors, tabBar, tabBarClearance, typography } from '../tokens';

describe('tabBarClearance', () => {
  it('keeps the floating bar, its offset and a gap free above the safe area', () => {
    expect(tabBarClearance(0)).toBe(tabBar.height + tabBar.bottomInset + 16);
    expect(tabBarClearance(24)).toBe(tabBarClearance(0) + 24);
  });
});

describe('typography', () => {
  it('uses the Plus Jakarta Sans family for every variant', () => {
    for (const style of Object.values(typography)) {
      expect(style.fontFamily).toMatch(/^PlusJakartaSans_/);
    }
  });
});

/** WCAG 2 contrast ratio between two `#RRGGBB` colours. */
function contrast(a: string, b: string): number {
  const luminance = (hex: string) => {
    const channel = (offset: number) => {
      const value = parseInt(hex.slice(offset, offset + 2), 16) / 255;
      return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
    };
    return 0.2126 * channel(1) + 0.7152 * channel(3) + 0.0722 * channel(5);
  };
  const [light, dark] = [luminance(a), luminance(b)].sort((x, y) => y - x) as [number, number];
  return (light + 0.05) / (dark + 0.05);
}

describe('palette "Indigo + Mangue"', () => {
  it.each([
    ['button label on the primary action', colors.onAccent, colors.accent],
    ['button label on a pressed primary action', colors.onAccent, colors.accentPressed],
    ['text on brand surfaces', colors.onPrimary, colors.primary],
    ['links on the background', colors.link, colors.background],
    ['links on a card', colors.link, colors.surface],
    ['text on the background', colors.ink, colors.background],
    ['secondary text on the background', colors.muted, colors.background],
    ['secondary text on a card', colors.muted, colors.surface],
    ['active tab on the tab bar', colors.tabActive, colors.tabBar],
    ['inactive tab on the tab bar', colors.tabInactive, colors.tabBar],
    ['info text on its tint', colors.infoInk, colors.infoSurface],
    ['error text on the background', colors.danger, colors.background],
  ])('%s is readable (4.5:1)', (_what, foreground, background) => {
    expect(contrast(foreground, background)).toBeGreaterThanOrEqual(4.5);
  });

  it('makes the primary action stand out from the dark brand surfaces (3:1)', () => {
    expect(contrast(colors.accent, colors.tabBar)).toBeGreaterThanOrEqual(3);
    expect(contrast(colors.accent, colors.primary)).toBeGreaterThanOrEqual(3);
  });

  it('labels the primary action with the brand indigo', () => {
    expect(colors.onAccent).toBe(colors.primary);
  });
});
