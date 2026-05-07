import { useEffect } from "react";
import { useStoreFrontConfig } from "@/features/admin/storefront/queries";
import { applyBrandVars } from "@/hooks/applyBrandVars";

export function useAdminBrandVars() {
  const { data: storeFront } = useStoreFrontConfig();

  useEffect(() => {
    if (!storeFront) return;
    applyBrandVars(storeFront);
  }, [storeFront]);
}
