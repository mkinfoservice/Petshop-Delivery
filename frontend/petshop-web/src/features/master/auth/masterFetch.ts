import { getMasterToken, clearMasterToken } from "./auth";

const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5082";

type FetchOptions = Omit<RequestInit, "headers"> & { headers?: Record<string, string> };

export async function masterFetch<T = unknown>(path: string, options: FetchOptions = {}): Promise<T> {
  const token = getMasterToken();

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers ?? {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const res = await fetch(`${API_URL}${path}`, { ...options, headers });

  if (res.status === 401) {
    clearMasterToken();
    window.location.href = "/master/login";
    throw new Error("Sessão expirada.");
  }

  if (!res.ok) {
    const ct = res.headers.get("content-type") ?? "";
    let msg = `Erro HTTP ${res.status}`;
    let body: unknown;
    try {
      if (ct.includes("application/json")) {
        body = await res.json();
        const data = body as Record<string, unknown>;
        msg = (data?.error ?? data?.message ?? JSON.stringify(data)) as string;
      } else {
        msg = await res.text();
      }
    } catch { /* ignore */ }
    const err = new Error(msg) as Error & { body?: unknown };
    err.body = body;
    throw err;
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
