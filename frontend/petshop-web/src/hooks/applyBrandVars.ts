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

type ApplyBrandVarsOptions = {
  respectDarkMode?: boolean;
};

export function applyBrandVars(storeFront: BrandPalette, options: ApplyBrandVarsOptions = {}) {
  const r = document.documentElement;
  const primary = storeFront.primaryColor ?? "#6366f1";
  const secondary = storeFront.secondaryColor ?? primary;
  const accent = storeFront.accentColor ?? "#f59e0b";
  const respectDarkMode = options.respectDarkMode ?? true;
  const isMegaPetFaimPalette =
    primary.toLowerCase() === "#ff5722" &&
    secondary.toLowerCase() === "#1e3a8a" &&
    accent.toLowerCase() === "#ffa726";

  r.style.setProperty("--brand", primary);
  r.style.setProperty("--brand-2", secondary);
  r.style.setProperty("--brand-accent", accent);
  r.style.setProperty("--accent", accent);

  if (respectDarkMode && r.classList.contains("dark")) {
    if (isMegaPetFaimPalette) {
      r.style.setProperty("--bg", "#0F1117");
      r.style.setProperty("--surface", "#181B24");
      r.style.setProperty("--surface-2", "#202431");
      r.style.setProperty("--border", "#2A3142");
      r.style.setProperty("--text", "#F8FAFC");
      r.style.setProperty("--text-muted", "#A7B4CC");
      return;
    }

    r.style.setProperty("--bg", `color-mix(in srgb, ${secondary} 82%, #050816)`);
    r.style.setProperty("--surface", `color-mix(in srgb, ${secondary} 64%, #ffffff)`);
    r.style.setProperty("--surface-2", `color-mix(in srgb, ${secondary} 52%, #ffffff)`);
    r.style.setProperty("--border", `color-mix(in srgb, ${secondary} 58%, #ffffff)`);
    r.style.setProperty("--text", "#f8fafc");
    r.style.setProperty("--text-muted", "rgba(248,250,252,0.68)");
    return;
  }

  r.style.setProperty("--bg", storeFront.bgColor ?? "#ffffff");
  r.style.setProperty("--surface", "#ffffff");
  r.style.setProperty("--surface-2", storeFront.surface2Color ?? "#f3f4f6");
  r.style.setProperty("--border", storeFront.borderColor ?? "rgba(0,0,0,0.08)");
  r.style.setProperty("--text", storeFront.textColor ?? "#111827");
  r.style.setProperty("--text-muted", storeFront.textMutedColor ?? "#6b7280");
}
