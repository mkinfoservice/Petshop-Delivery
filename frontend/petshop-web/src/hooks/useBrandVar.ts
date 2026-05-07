import { useEffect } from "react";
import { useStoreFront } from "@/features/catalog/queries";
import { applyBrandVars } from "@/hooks/applyBrandVars";

/**
 * Aplica toda a paleta da loja como CSS variables no <html>.
 * Usar em páginas fora do App.tsx (Checkout, ProductDetail) para
 * garantir que as variáveis estejam disponíveis na navegação direta.
 */
export function useBrandVar() {
  const { data: storeFront } = useStoreFront();

  useEffect(() => {
    if (!storeFront) return;
    const apply = () => applyBrandVars(storeFront, { respectDarkMode: false });
    apply();
    window.addEventListener("themechange", apply);
    return () => window.removeEventListener("themechange", apply);
  }, [storeFront]);
}
