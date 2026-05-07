type BrandPalette = {
  primaryColor?: string | null;
  secondaryColor?: string | null;
  accentColor?: string | null;
  bgColor?: string | null;
  surface2Color?: string | null;
  borderColor?: string | null;
  textColor?: string | null;
  textMutedColor?: string | null;
};

export function applyBrandVars(storeFront: BrandPalette) {
  const r = document.documentElement;
  r.style.setProperty("--brand", storeFront.primaryColor ?? "#6366f1");
  r.style.setProperty("--brand-2", storeFront.secondaryColor ?? "#6366f1");
  r.style.setProperty("--brand-accent", storeFront.accentColor ?? "#f59e0b");
  r.style.setProperty("--bg", storeFront.bgColor ?? "#ffffff");
  r.style.setProperty("--surface-2", storeFront.surface2Color ?? "#f3f4f6");
  r.style.setProperty("--border", storeFront.borderColor ?? "rgba(0,0,0,0.08)");
  r.style.setProperty("--text", storeFront.textColor ?? "#111827");
  r.style.setProperty("--text-muted", storeFront.textMutedColor ?? "#6b7280");
}
