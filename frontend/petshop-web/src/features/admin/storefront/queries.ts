import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as api from "./api";
import type { UpsertBannerSlideRequest, UpdateStoreFrontConfigRequest } from "./types";

const QK = ["storefront-config"] as const;
const BRANDING_HEALTH_QK = ["storefront-branding-health"] as const;

export function useStoreFrontConfig() {
  return useQuery({ queryKey: QK, queryFn: api.fetchStoreFrontConfig });
}

export function useBrandingHealth() {
  return useQuery({ queryKey: BRANDING_HEALTH_QK, queryFn: api.fetchBrandingHealth });
}

export function useUpdateStoreFrontConfig() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateStoreFrontConfigRequest) => api.updateStoreFrontConfig(req),
    onSuccess: (data) => {
      qc.setQueryData(QK, data);
      qc.invalidateQueries({ queryKey: BRANDING_HEALTH_QK });
      qc.invalidateQueries({ queryKey: ["storefront"] });
    },
  });
}

export function useAddSlide() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: UpsertBannerSlideRequest) => api.addSlide(req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: QK });
      qc.invalidateQueries({ queryKey: BRANDING_HEALTH_QK });
    },
  });
}

export function useUpdateSlide() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpsertBannerSlideRequest }) =>
      api.updateSlide(id, req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: QK });
      qc.invalidateQueries({ queryKey: BRANDING_HEALTH_QK });
    },
  });
}

export function useDeleteSlide() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.deleteSlide(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: QK });
      qc.invalidateQueries({ queryKey: BRANDING_HEALTH_QK });
    },
  });
}

export function useReorderSlides() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (orderedIds: string[]) => api.reorderSlides(orderedIds),
    onSuccess: (data) => {
      qc.setQueryData(QK, data);
      qc.invalidateQueries({ queryKey: BRANDING_HEALTH_QK });
    },
  });
}
