import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { delivererLogin } from "@/features/deliverer/auth/api";
import {
  isDelivererAuthenticated,
  saveDelivererToken,
  saveDelivererInfo,
} from "@/features/deliverer/auth/auth";

const PIN_MIN_LENGTH = 4;
const PIN_MAX_LENGTH = 6;

export default function DelivererLogin() {
  const navigate = useNavigate();
  const [phone, setPhone] = useState("");
  const [pin, setPin] = useState<string[]>(Array(PIN_MAX_LENGTH).fill(""));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  useEffect(() => {
    if (isDelivererAuthenticated()) {
      navigate("/deliverer", { replace: true });
    }
  }, [navigate]);

  function handlePinChange(index: number, value: string) {
    const digit = value.replace(/\D/g, "").slice(-1);
    const next = [...pin];
    next[index] = digit;
    setPin(next);
    if (digit && index < PIN_MAX_LENGTH - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  }

  function handlePinKeyDown(index: number, e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Backspace" && !pin[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
    if (e.key === "Enter") handleLogin();
  }

  async function handleLogin() {
    if (loading) return;
    const p = phone.trim();
    const pi = pin.join("");
    if (!p || pi.length < PIN_MIN_LENGTH) {
      setError("Informe o telefone e o PIN com 4 a 6 dígitos.");
      return;
    }
    try {
      setLoading(true);
      setError("");
      const res = await delivererLogin({ phone: p, pin: pi });
      saveDelivererToken(res.token);
      saveDelivererInfo({ id: res.delivererId, name: res.name });
      navigate("/deliverer", { replace: true });
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Erro ao entrar.");
    } finally {
      setLoading(false);
    }
  }

  const pinFilled = pin.join("").length >= PIN_MIN_LENGTH;
  const canSubmit = phone.trim().length > 0 && pinFilled && !loading;

  return (
    <div
      className="min-h-dvh flex items-center justify-center px-4"
      style={{ backgroundColor: "var(--bg)" }}
    >
      <div
        className="w-full max-w-sm rounded-2xl border shadow-2xl"
        style={{ backgroundColor: "var(--surface)", borderColor: "var(--border)" }}
      >
        {/* Header */}
        <div className="px-8 pt-8 pb-6 flex flex-col items-center gap-3">
          <div
            className="w-14 h-14 rounded-2xl flex items-center justify-center shadow-[0_0_28px_rgba(124,92,248,0.5)]"
            style={{ backgroundColor: "var(--brand)" }}
          >
            <span className="text-white text-2xl select-none" aria-hidden="true">🐾</span>
          </div>
          <div className="text-center">
            <h1 className="text-xl font-bold tracking-tight" style={{ color: "var(--text)" }}>
              Portal do Entregador
            </h1>
            <p className="text-sm mt-0.5" style={{ color: "var(--text-muted)" }}>
              Entre com telefone e PIN
            </p>
          </div>
        </div>

        {/* Divider */}
        <div className="h-px mx-6" style={{ backgroundColor: "var(--border)" }} />

        {/* Form */}
        <div className="px-8 pt-6 pb-8 space-y-5">
          {/* Phone */}
          <div className="space-y-1.5">
            <label
              htmlFor="phone"
              className="block text-xs font-semibold uppercase tracking-widest"
              style={{ color: "var(--text-muted)" }}
            >
              Telefone
            </label>
            <input
              id="phone"
              type="tel"
              autoComplete="tel"
              inputMode="tel"
              required
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="(21) 99999-9999"
              disabled={loading}
              className="w-full h-11 rounded-xl border px-3.5 text-sm outline-none transition-all placeholder:opacity-40 focus:ring-2 focus:ring-[#7c5cf8]/40 disabled:opacity-60"
              style={{
                backgroundColor: "var(--surface-2)",
                borderColor: "var(--border)",
                color: "var(--text)",
              }}
            />
          </div>

          {/* PIN circles */}
          <div className="space-y-1.5">
            <label
              className="block text-xs font-semibold uppercase tracking-widest"
              style={{ color: "var(--text-muted)" }}
            >
              PIN
            </label>
            <div className="flex gap-3 justify-center">
              {Array.from({ length: PIN_MAX_LENGTH }).map((_, i) => (
                <input
                  key={i}
                  ref={(el) => { inputRefs.current[i] = el; }}
                  type="password"
                  inputMode="numeric"
                  maxLength={1}
                  value={pin[i]}
                  onChange={(e) => handlePinChange(i, e.target.value)}
                  onKeyDown={(e) => handlePinKeyDown(i, e)}
                  disabled={loading}
                  className="w-11 h-12 sm:w-12 sm:h-12 rounded-xl border text-center text-xl font-bold outline-none transition-all focus:ring-2 focus:ring-[#7c5cf8]/50 disabled:opacity-60"
                  style={{
                    backgroundColor: pin[i] ? "#7c5cf8" : "var(--surface-2)",
                    borderColor: pin[i] ? "#7c5cf8" : "var(--border)",
                    color: pin[i] ? "#fff" : "transparent",
                    caretColor: "transparent",
                  }}
                />
              ))}
            </div>
          </div>
          <p className="text-xs text-center" style={{ color: "var(--text-muted)" }}>
            PIN de 4 a 6 dígitos
          </p>

          {/* Error */}
          {error && (
            <div className="rounded-xl border border-red-800 bg-red-950/40 px-3.5 py-2.5 text-sm text-red-300">
              {error}
            </div>
          )}

          {/* Submit */}
          <button
            type="button"
            disabled={!canSubmit}
            onClick={handleLogin}
            className="w-full h-11 rounded-xl text-sm font-bold text-white transition-all outline-none focus:ring-2 focus:ring-[#7c5cf8]/50 disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ background: "linear-gradient(135deg, #7c5cf8 0%, #9b7efa 100%)" }}
          >
            {loading ? "Entrando…" : "Entrar"}
          </button>
        </div>
      </div>
    </div>
  );
}
