import { useEffect, useState } from "react";
import { Coffee } from "lucide-react";
import { getCashRegisters, openSession, type CashRegister } from "@/features/pdv/api";
import { usePdv } from "@/features/pdv/PdvContext";


interface Props {
  onOpened: () => void;
}

export default function OpenSessionPage({ onOpened }: Props) {
  const { refreshSession } = usePdv();
  const [registers, setRegisters]   = useState<CashRegister[]>([]);
  const [selected, setSelected]     = useState<string>("");
  const [opening, setOpening]       = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError]           = useState<string | null>(null);

  useEffect(() => {
    getCashRegisters().then((rs) => {
      const active = rs.filter((r) => r.isActive);
      setRegisters(active);
      if (active.length === 1) setSelected(active[0].id);
    });
  }, []);

  async function handleOpen() {
    if (!selected) { setError("Selecione um terminal."); return; }
    setSubmitting(true);
    setError(null);
    try {
      await openSession({ cashRegisterId: selected, openingBalanceCents: opening });
      await refreshSession();
      onOpened();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Erro ao abrir sessão.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4" style={{ background: "var(--bg)" }}>
      <div className="w-full max-w-sm space-y-6">
        {/* Header */}
        <div className="text-center space-y-3">
          <div className="w-16 h-16 rounded-3xl flex items-center justify-center mx-auto"
            style={{ background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))", boxShadow: "0 8px 32px rgba(0,0,0,0.18)" }}>
            <Coffee size={28} style={{ color: "var(--brand-accent)" }} />
          </div>
          <div>
            <h1 className="text-2xl font-black" style={{ color: "var(--text)" }}>Abrir Caixa</h1>
            <p className="text-sm mt-0.5" style={{ color: "var(--text-muted)", opacity: 0.65 }}>Frente de Caixa</p>
          </div>
        </div>

        <div className="rounded-3xl p-6 space-y-5 shadow-sm"
          style={{ background: "var(--surface)", boxShadow: "0 4px 24px rgba(0,0,0,0.07)" }}>
          <div className="space-y-1.5">
            <label className="text-[11px] font-bold uppercase tracking-wider" style={{ color: "var(--text-muted)", opacity: 0.7 }}>Terminal</label>
            <select
              className="w-full rounded-xl px-3 py-3 text-sm focus:outline-none appearance-none"
              style={{ border: "1.5px solid var(--border)", color: "var(--text)", background: "var(--bg)" }}
              value={selected}
              onChange={(e) => setSelected(e.target.value)}
            >
              <option value="">Selecione...</option>
              {registers.map((r) => (
                <option key={r.id} value={r.id}>{r.name}</option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <label className="text-[11px] font-bold uppercase tracking-wider" style={{ color: "var(--text-muted)", opacity: 0.7 }}>Fundo de caixa (R$)</label>
            <input
              type="number" min={0} step={0.01}
              className="w-full rounded-xl px-3 py-3 text-sm focus:outline-none"
              style={{ border: "1.5px solid var(--border)", color: "var(--text)", background: "var(--bg)" }}
              value={(opening / 100).toFixed(2)}
              onChange={(e) => setOpening(Math.round(parseFloat(e.target.value || "0") * 100))}
            />
          </div>

          {error && <p className="text-red-500 text-sm text-center">{error}</p>}

          <button
            className="w-full py-4 rounded-2xl font-black text-white text-base transition active:scale-95 disabled:opacity-40"
            style={{ background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))", boxShadow: "0 4px 18px rgba(0,0,0,0.22)" }}
            disabled={submitting || !selected}
            onClick={handleOpen}
          >
            {submitting ? "Abrindo..." : "Abrir Caixa"}
          </button>

          {registers.length === 0 && (
            <p className="text-center text-xs" style={{ color: "var(--text-muted)", opacity: 0.5 }}>
              Nenhum terminal ativo. Configure em Admin → Terminais.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
