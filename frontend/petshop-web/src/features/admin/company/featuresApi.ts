import { adminFetch } from "@/features/admin/auth/adminFetch";

export type CompanyFeatures = Record<string, boolean>;

export function fetchCompanyFeatures(): Promise<{ features: CompanyFeatures }> {
  return adminFetch<{ features: CompanyFeatures }>("/admin/company/features");
}
