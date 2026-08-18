import { useState } from "react";
import { Link } from "react-router-dom";
import { KeyRound } from "lucide-react";
import { forgotPassword } from "@/features/admin/auth/api";
import { resolveActiveTenantSlugSync } from "@/utils/tenant";

export default function ForgotPasswordPage() {
  const [identifier, setIdentifier] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [sent, setSent] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const value = identifier.trim();
    if (!value) { setError("Informe seu usuário ou e-mail."); return; }

    const slug = resolveActiveTenantSlugSync();
    if (!slug) {
      setError("Não foi possível identificar a empresa deste endereço.");
      return;
    }

    try {
      setLoading(true);
      setError("");
      await forgotPassword(value, slug);
      setSent(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Erro ao solicitar redefinição.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-dvh flex items-center justify-center px-4" style={{ background: "var(--bg)" }}>
      <div className="w-full max-w-sm rounded-3xl shadow-xl overflow-hidden"
        style={{ background: "var(--surface)", boxShadow: "0 8px 40px rgba(0,0,0,0.10)" }}>

        <div className="px-8 pt-10 pb-7 flex flex-col items-center gap-4"
          style={{ background: "linear-gradient(160deg, var(--brand) 0%, color-mix(in srgb, var(--brand) 72%, #000) 100%)" }}>
          <div className="w-16 h-16 rounded-2xl flex items-center justify-center"
            style={{ background: "color-mix(in srgb, var(--brand-accent) 20%, transparent)", boxShadow: "0 0 0 1px color-mix(in srgb, var(--brand-accent) 25%, transparent)" }}>
            <KeyRound size={30} style={{ color: "var(--brand-accent)" }} />
          </div>
          <div className="text-center">
            <h1 className="text-xl font-black text-white">Esqueci minha senha</h1>
            <p className="text-sm mt-0.5" style={{ color: "rgba(255,255,255,0.55)" }}>
              Vamos te enviar um link de redefinição
            </p>
          </div>
        </div>

        <div className="px-8 pt-7 pb-8" style={{ background: "var(--bg)" }}>
          {sent ? (
            <div className="space-y-4">
              <div className="rounded-xl px-3.5 py-3 text-sm"
                style={{ background: "rgba(34,197,94,0.1)", color: "#16a34a", border: "1px solid rgba(34,197,94,0.2)" }}>
                Se existir uma conta com esses dados, você vai receber um e-mail com o link de redefinição em alguns minutos.
              </div>
              <Link to="/login" className="block text-center text-sm font-semibold hover:underline" style={{ color: "var(--brand)" }}>
                Voltar para o login
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-1.5">
                <label htmlFor="identifier"
                  className="block text-[11px] font-bold uppercase tracking-widest"
                  style={{ color: "var(--text-muted)", opacity: 0.7 }}>
                  Usuário ou e-mail
                </label>
                <input
                  id="identifier" type="text" autoComplete="username" required
                  value={identifier} onChange={(e) => setIdentifier(e.target.value)}
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

              {error && (
                <div className="rounded-xl px-3.5 py-2.5 text-sm"
                  style={{ background: "rgba(239,68,68,0.1)", color: "#dc2626", border: "1px solid rgba(239,68,68,0.2)" }}>
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={loading || !identifier.trim()}
                className="w-full h-12 rounded-2xl text-sm font-black text-white transition-all active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed"
                style={{ background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))",
                  boxShadow: "0 4px 18px rgba(0,0,0,0.22)" }}
              >
                {loading ? "Enviando…" : "Enviar link de redefinição"}
              </button>

              <Link to="/login" className="block text-center text-sm font-semibold hover:underline" style={{ color: "var(--text-muted)" }}>
                Voltar para o login
              </Link>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
