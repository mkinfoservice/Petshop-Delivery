const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5082";

export type LoginRequest = {
  username: string;
  password: string;
  slug?: string | null;
};

export type LoginResponse = {
  token: string;
};

export async function login(payload: LoginRequest): Promise<LoginResponse> {
  const r = await fetch(`${API_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!r.ok) {
    const text = await r.text().catch(() => "");
    throw new Error(text || "Credenciais inválidas.");
  }

  return r.json();
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const r = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!r.ok) {
    let msg = "Erro ao processar solicitação.";
    try {
      const data = (await r.json()) as Record<string, unknown>;
      msg = (data?.error as string) ?? (data?.message as string) ?? msg;
    } catch { /* ignore */ }
    throw new Error(msg);
  }

  return r.json();
}

export function forgotPassword(identifier: string, slug: string) {
  return postJson<{ message: string }>("/auth/forgot-password", { identifier, slug });
}

export function resetPassword(token: string, newPassword: string) {
  return postJson<{ message: string }>("/auth/reset-password", { token, newPassword });
}
