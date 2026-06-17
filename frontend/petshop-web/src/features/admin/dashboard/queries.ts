import { useQuery } from "@tanstack/react-query";
import { fetchDashboard } from "./api";

export function useDashboard() {
  return useQuery({
    queryKey: ["admin-dashboard"],
    queryFn: fetchDashboard,
    refetchInterval: 60_000,
    staleTime: 60_000,
  });
}
