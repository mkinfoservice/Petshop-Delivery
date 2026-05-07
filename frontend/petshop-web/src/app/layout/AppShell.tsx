import type { ReactNode } from "react";
import { AppHeader } from "./AppHeader";
import { AppSidebar } from "./AppSidebar";
import { SupplyAlertsPopup } from "./SupplyAlertsPopup";
import { useAdminBrandVars } from "@/hooks/useAdminBrandVars";

interface Props {
  children: ReactNode;
}

export function AppShell({ children }: Props) {
  useAdminBrandVars();

  return (
    <div
      className="min-h-dvh flex flex-col"
      style={{ backgroundColor: "var(--bg)" }}
    >
      <AppHeader />
      {/* Body: sidebar + content. Cada page gerencia seu próprio layout/padding. */}
      <div className="flex flex-1 min-w-0">
        <AppSidebar />
        <main className="flex-1 min-w-0">{children}</main>
      </div>
      <SupplyAlertsPopup />
    </div>
  );
}
