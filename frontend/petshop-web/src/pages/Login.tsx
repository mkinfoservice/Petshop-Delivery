import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Coffee } from "lucide-react";
import { login } from "@/features/admin/auth/api";
import { isAuthenticated, saveToken } from "@/features/admin/auth/auth";
import { resolveTenantFromHost, fetchTenantInfo } from "@/utils/tenant";


export default function LoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const tenantSlug = resolveTenantFromHost();
  const tenantQuery = useQuery({
    queryKey: ["tenant"],
    queryFn: fetchTenantInfo,
    enabled: !!tenantSlug,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
  const companyName = tenantQuery.data?.name;

  useEffect(() => {
    if (isAuthenticated()) navigate("/app", { replace: true });
  }, [navigate]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const u = username.trim();
    const p = password;
    if (!u || !p) { setError("Informe usuário e senha."); return; }
    try {
      setLoading(true);
      setError("");
      const res = await login({ username: u, password: p });
      saveToken(res.token);
      navigate("/app", { replace: true });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Erro ao entrar.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-dvh flex items-center justify-center px-4" style={{ background: "var(--bg)" }}>
      <div className="w-full max-w-sm rounded-3xl shadow-xl overflow-hidden"
        style={{ background: "var(--surface)", boxShadow: "0 8px 40px rgba(0,0,0,0.10)" }}>

        {/* Header */}
        <div className="px-8 pt-10 pb-7 flex flex-col items-center gap-4"
          style={{ background: "linear-gradient(160deg, var(--brand) 0%, color-mix(in srgb, var(--brand) 72%, #000) 100%)" }}>
          <div className="w-16 h-16 rounded-2xl flex items-center justify-center"
            style={{ background: "color-mix(in srgb, var(--brand-accent) 20%, transparent)", boxShadow: "0 0 0 1px color-mix(in srgb, var(--brand-accent) 25%, transparent)" }}>
            <Coffee size={30} style={{ color: "var(--brand-accent)" }} />
          </div>
          <div className="text-center">
            <h1 className="text-xl font-black text-white">
              {companyName ?? "vendApps"}
            </h1>
            <p className="text-sm mt-0.5" style={{ color: "rgba(255,255,255,0.55)" }}>
              Central de Operações
            </p>
          </div>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="px-8 pt-7 pb-8 space-y-4" style={{ background: "var(--bg)" }}>
          <div className="space-y-1.5">
            <label htmlFor="username"
              className="block text-[11px] font-bold uppercase tracking-widest"
              style={{ color: "var(--text-muted)", opacity: 0.7 }}>
              Usuário
            </label>
            <input
              id="username" type="text" autoComplete="username" required
              value={username} onChange={(e) => setUsername(e.target.value)}
              placeholder="admin" disabled={loading}
              className="w-full h-11 rounded-xl px-3.5 text-sm outline-none transition-all placeholder:opacity-40 focus:ring-2 disabled:opacity-60"
              style={{
                border: "1.5px solid var(--border)",
                backgroundColor: "var(--surface-2)",
                color: "var(--text)",
                ["--tw-ring-color" as string]: "color-mix(in srgb, var(--brand-accent) 30%, transparent)",
              }}
            />
          </div>

          <div className="space-y-1.5">
            <label htmlFor="password"
              className="block text-[11px] font-bold uppercase tracking-widest"
              style={{ color: "var(--text-muted)", opacity: 0.7 }}>
              Senha
            </label>
            <input
              id="password" type="password" autoComplete="current-password" required
              value={password} onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••" disabled={loading}
              className="w-full h-11 rounded-xl px-3.5 text-sm outline-none transition-all placeholder:opacity-40 focus:ring-2 disabled:opacity-60"
              style={{
                border: "1.5px solid var(--border)",
                backgroundColor: "var(--surface-2)",
                color: "var(--text)",
                ["--tw-ring-color" as string]: "color-mix(in srgb, var(--brand-accent) 30%, transparent)",
              }}
            />
          </div>

          {error && (
            <div className="rounded-xl px-3.5 py-2.5 text-sm"
              style={{ background: "rgba(239,68,68,0.1)", color: "#dc2626", border: "1px solid rgba(239,68,68,0.2)" }}>
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading || !username.trim() || !password}
            className="w-full h-12 rounded-2xl text-sm font-black text-white transition-all active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))",
              boxShadow: "0 4px 18px rgba(0,0,0,0.22)" }}
          >
            {loading ? "Entrando…" : "Entrar"}
          </button>
        </form>
      </div>
    </div>
  );
}
