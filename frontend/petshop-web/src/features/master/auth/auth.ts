const TOKEN_KEY = "master_admin_token";

export function saveMasterToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function getMasterToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function clearMasterToken() {
  localStorage.removeItem(TOKEN_KEY);
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

export function isMasterAuthenticated(): boolean {
  const token = getMasterToken();
  if (!token) return false;
  if (isJwtExpired(token)) {
    clearMasterToken();
    return false;
  }
  return true;
}
