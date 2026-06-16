const TOKEN_KEY = "petshop_deliverer_token";
const INFO_KEY = "petshop_deliverer_info";

export function saveDelivererToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function getDelivererToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function clearDelivererToken() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(INFO_KEY);
}

function isJwtExpired(token: string): boolean {
  try {
    const parts = token.split(".");
    if (parts.length !== 3) return true;
    const b64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const payload = JSON.parse(atob(b64)) as { exp?: number };
    if (!payload.exp) return false;
    return Math.floor(Date.now() / 1000) >= payload.exp;
  } catch {
    return true;
  }
}

export function isDelivererAuthenticated(): boolean {
  const token = getDelivererToken();
  if (!token) return false;
  if (isJwtExpired(token)) {
    clearDelivererToken();
    return false;
  }
  return true;
}

export function saveDelivererInfo(info: { id: string; name: string }) {
  localStorage.setItem(INFO_KEY, JSON.stringify(info));
}

export function getDelivererInfo(): { id: string; name: string } | null {
  const raw = localStorage.getItem(INFO_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}
