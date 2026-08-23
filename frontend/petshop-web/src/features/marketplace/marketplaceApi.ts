import { adminFetch } from "@/features/admin/auth/adminFetch";

// ── Types ─────────────────────────────────────────────────────────────────────

export interface MarketplaceIntegrationDto {
  id: string;
  type: string;           // "IFood"
  merchantId: string;
  displayName: string;
  clientId: string;
  webhookSecret: string | null;
  autoAcceptOrders: boolean;
  autoPrint: boolean;
  isActive: boolean;
  createdAtUtc: string;
  lastOrderReceivedAtUtc: string | null;
  lastCatalogSyncAtUtc: string | null;
  lastErrorMessage: string | null;
  catalogSyncMode: string; // NotConfigured | AllProducts | SelectedCategories | SelectedProducts
  webhookUrl: string;     // URL relativa para configurar no portal iFood
}

export interface CatalogScope {
  mode: string;
  categoryIds: string[];
  productIds: string[];
}

export interface UpsertCatalogScopeRequest {
  mode: string;
  categoryIds?: string[];
  productIds?: string[];
}

export interface UpsertIntegrationRequest {
  type: number;           // 1 = IFood
  merchantId: string;
  clientId: string;
  clientSecret: string;
  displayName: string | null;
  webhookSecret: string | null;
  autoAcceptOrders: boolean;
  autoPrint: boolean;
}

export interface CatalogSyncResult {
  updated: number;
  skipped: number;
  failed: number;
  notFound: string[];
  errorMessage: string | null;
}

// ── API calls ─────────────────────────────────────────────────────────────────

export async function listIntegrations(): Promise<MarketplaceIntegrationDto[]> {
  return adminFetch<MarketplaceIntegrationDto[]>("/admin/marketplace");
}

export async function getIntegration(id: string): Promise<MarketplaceIntegrationDto> {
  return adminFetch<MarketplaceIntegrationDto>(`/admin/marketplace/${id}`);
}

export async function createIntegration(
  req: UpsertIntegrationRequest,
): Promise<MarketplaceIntegrationDto> {
  return adminFetch<MarketplaceIntegrationDto>("/admin/marketplace", {
    method: "POST",
    body: JSON.stringify(req),
  });
}

export async function updateIntegration(
  id: string,
  req: UpsertIntegrationRequest,
): Promise<MarketplaceIntegrationDto> {
  return adminFetch<MarketplaceIntegrationDto>(`/admin/marketplace/${id}`, {
    method: "PUT",
    body: JSON.stringify(req),
  });
}

export async function deactivateIntegration(id: string): Promise<void> {
  await adminFetch<void>(`/admin/marketplace/${id}`, { method: "DELETE" });
}

export async function syncCatalog(id: string): Promise<CatalogSyncResult> {
  return adminFetch<CatalogSyncResult>(`/admin/marketplace/${id}/sync-catalog`, {
    method: "POST",
  });
}

// ── Mercado Livre (OAuth) ──────────────────────────────────────────────────────
// Fluxo separado do CRUD genérico acima: o vendedor autoriza no próprio
// Mercado Livre em vez de digitar client id/secret manualmente (como no iFood).

export async function getCatalogScope(id: string): Promise<CatalogScope> {
  return adminFetch<CatalogScope>(`/admin/marketplace/${id}/catalog-scope`);
}

export async function setCatalogScope(
  id: string,
  req: UpsertCatalogScopeRequest,
): Promise<CatalogScope> {
  return adminFetch<CatalogScope>(`/admin/marketplace/${id}/catalog-scope`, {
    method: "PUT",
    body: JSON.stringify(req),
  });
}

export async function startMercadoLivreConnect(): Promise<string> {
  const res = await adminFetch<{ authorizeUrl: string }>(
    "/api/integrations/mercadolivre/authorize",
  );
  return res.authorizeUrl;
}
