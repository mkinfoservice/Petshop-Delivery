import { adminFetch } from "@/features/admin/auth/adminFetch";
import type {
  OperationalAuditDetailResponse,
  OperationalAuditFilters,
  OperationalAuditListResponse,
} from "./types";

function buildQuery(filters: OperationalAuditFilters) {
  const params = new URLSearchParams();

  Object.entries(filters).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") return;
    params.set(key, String(value));
  });

  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

export function fetchOperationalAudit(filters: OperationalAuditFilters) {
  return adminFetch<OperationalAuditListResponse>(
    `/admin/audit/operational${buildQuery(filters)}`,
  );
}

export function fetchOperationalAuditDetail(id: string) {
  return adminFetch<OperationalAuditDetailResponse>(
    `/admin/audit/operational/${encodeURIComponent(id)}`,
  );
}
