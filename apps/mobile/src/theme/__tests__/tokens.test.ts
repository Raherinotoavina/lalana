import { tabBar, tabBarClearance, typography } from '../tokens';

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
