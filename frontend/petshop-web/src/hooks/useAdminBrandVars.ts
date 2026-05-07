import { useEffect } from "react";
import { useStoreFrontConfig } from "@/features/admin/storefront/queries";
import { applyBrandVars } from "@/hooks/applyBrandVars";

export function useAdminBrandVars() {
  const { data: storeFront } = useStoreFrontConfig();

  useEffect(() => {
    if (!storeFront) return;
    const apply = () => applyBrandVars(storeFront);
    apply();
    window.addEventListener("themechange", apply);
    return () => window.removeEventListener("themechange", apply);
  }, [storeFront]);
}
