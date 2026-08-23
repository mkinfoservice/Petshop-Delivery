import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  approveName,
  approveAllNames,
  approveImage,
  approveDescription,
  bulkApproveNames,
  bulkApproveDescriptions,
  clearAllImages,
  createBatch,
  fetchBatch,
  fetchBatches,
  fetchConfig,
  fetchPendingImages,
  fetchPendingNames,
  fetchPendingDescriptions,
  normalizeCategories,
  rejectImage,
  rejectName,
  rejectDescription,
  reprocessWithoutImage,
  updateConfig,
} from "./api";
import type { CreateEnrichmentBatchRequest, UpdateEnrichmentConfigRequest } from "./types";

const KEYS = {
  batches: ["enrichment-batches"] as const,
  batch: (id: string) => ["enrichment-batch", id] as const,
  pendingNames: (page: number) => ["enrichment-pending-names", page] as const,
  pendingImages: (page: number) => ["enrichment-pending-images", page] as const,
  pendingDescriptions: (page: number) => ["enrichment-pending-descriptions", page] as const,
  config: ["enrichment-config"] as const,
};

// ── Batches ────────────────────────────────────────────────────────────────────

export function useEnrichmentBatches(page = 1) {
  return useQuery({
    queryKey: [...KEYS.batches, page],
    queryFn: () => fetchBatches(page),
    // Atualiza a cada 4s enquanto algum lote estiver ativo
    refetchInterval: (query) => {
      const items = query.state.data?.items ?? [];
      const hasActive = items.some(
        (b) => b.status === "Queued" || b.status === "Running"
      );
      return hasActive ? 4000 : false;
    },
  });
}

export function useEnrichmentBatch(id: string | null) {
  return useQuery({
    queryKey: KEYS.batch(id ?? ""),
    queryFn: () => fetchBatch(id!),
    enabled: !!id,
    refetchInterval: (query) => {
      const s = query.state.data?.status;
      return s === "Queued" || s === "Running" ? 3000 : false;
    },
  });
}

export function useCreateBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateEnrichmentBatchRequest) => createBatch(req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.batches });
    },
  });
}

export function useReprocessWithoutImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: reprocessWithoutImage,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.batches });
    },
  });
}

export function useClearAllImages() {
  return useMutation({ mutationFn: clearAllImages });
}

// ── Name suggestions ───────────────────────────────────────────────────────────

export function usePendingNames(page = 1) {
  return useQuery({
    queryKey: KEYS.pendingNames(page),
    queryFn: () => fetchPendingNames(page),
  });
}

export function useApproveName() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: approveName,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-names"] });
    },
  });
}

export function useRejectName() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: rejectName,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-names"] });
    },
  });
}

export function useBulkApproveNames() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ids: string[]) => bulkApproveNames(ids),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-names"] });
    },
  });
}

export function useApproveAllNames() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: approveAllNames,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-names"] });
    },
  });
}

export function useNormalizeCategories() {
  return useMutation({ mutationFn: normalizeCategories });
}

// ── Image candidates ───────────────────────────────────────────────────────────

export function usePendingImages(page = 1) {
  return useQuery({
    queryKey: KEYS.pendingImages(page),
    queryFn: () => fetchPendingImages(page),
  });
}

export function useApproveImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: approveImage,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-images"] });
    },
  });
}

export function useRejectImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: rejectImage,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-images"] });
    },
  });
}

// ── Description suggestions ─────────────────────────────────────────────────────

export function usePendingDescriptions(page = 1) {
  return useQuery({
    queryKey: KEYS.pendingDescriptions(page),
    queryFn: () => fetchPendingDescriptions(page),
  });
}

export function useApproveDescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: approveDescription,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-descriptions"] });
    },
  });
}

export function useRejectDescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: rejectDescription,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-descriptions"] });
    },
  });
}

export function useBulkApproveDescriptions() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ids: string[]) => bulkApproveDescriptions(ids),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["enrichment-pending-descriptions"] });
    },
  });
}

// ── Config ─────────────────────────────────────────────────────────────────────

export function useEnrichmentConfig() {
  return useQuery({
    queryKey: KEYS.config,
    queryFn: fetchConfig,
  });
}

export function useUpdateEnrichmentConfig() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateEnrichmentConfigRequest) => updateConfig(req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.config });
    },
  });
}
