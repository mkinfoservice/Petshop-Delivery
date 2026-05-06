import { useEffect } from "react";
import { useStoreFront } from "@/features/catalog/queries";

/**
 * Aplica toda a paleta da loja como CSS variables no <html>.
 * Usar em páginas fora do App.tsx (Checkout, ProductDetail) para
 * garantir que as variáveis estejam disponíveis na navegação direta.
 */
export function useBrandVar() {
  const { data: storeFront } = useStoreFront();

  useEffect(() => {
    if (!storeFront) return;
    const r = document.documentElement;
    r.style.setProperty("--brand",        storeFront.primaryColor   ?? "#6366f1");
    r.style.setProperty("--brand-2",      storeFront.secondaryColor ?? "#6366f1");
    r.style.setProperty("--brand-accent", storeFront.accentColor    ?? "#f59e0b");
    r.style.setProperty("--bg",           storeFront.bgColor        ?? "#ffffff");
    r.style.setProperty("--surface-2",    storeFront.surface2Color  ?? "#f3f4f6");
    r.style.setProperty("--border",       storeFront.borderColor    ?? "rgba(0,0,0,0.08)");
    r.style.setProperty("--text",         storeFront.textColor      ?? "#111827");
    r.style.setProperty("--text-muted",   storeFront.textMutedColor ?? "#6b7280");
  }, [storeFront]);
}
