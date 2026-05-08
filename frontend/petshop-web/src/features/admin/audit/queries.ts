import { useQuery } from "@tanstack/react-query";
import * as api from "./api";
import type { OperationalAuditFilters } from "./types";

export function useOperationalAudit(filters: OperationalAuditFilters) {
  return useQuery({
    queryKey: ["operational-audit", filters],
    queryFn: () => api.fetchOperationalAudit(filters),
  });
}

export function useOperationalAuditDetail(id: string | null) {
  return useQuery({
    queryKey: ["operational-audit-detail", id],
    queryFn: () => api.fetchOperationalAuditDetail(id!),
    enabled: Boolean(id),
  });
}
