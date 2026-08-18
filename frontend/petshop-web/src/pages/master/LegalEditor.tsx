import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ShieldCheck, LogOut, ChevronLeft } from "lucide-react";
import { clearMasterToken } from "@/features/master/auth/auth";
import {
  fetchActiveLegalDocument,
  fetchLegalDocumentHistory,
  publishLegalDocument,
  type LegalDocumentType,
} from "@/features/master/legal/api";

const TABS: { key: LegalDocumentType; label: string }[] = [
  { key: "terms", label: "Termos de Uso" },
  { key: "privacy", label: "Política de Privacidade" },
];

export default function LegalEditorPage() {
  const [type, setType] = useState<LegalDocumentType>("terms");
  const qc = useQueryClient();

  const { data: active, isLoading } = useQuery({
    queryKey: ["master", "legal", type],
    queryFn: () => fetchActiveLegalDocument(type),
  });

  const { data: history } = useQuery({
    queryKey: ["master", "legal", type, "history"],
    queryFn: () => fetchLegalDocumentHistory(type),
  });

  const [version, setVersion] = useState("");
  const [content, setContent] = useState("");
  const [saved, setSaved] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    setVersion("");
    setContent(active?.content ?? "");
    setSaved(false);
    setErr(null);
  }, [active, type]);

  const publishMut = useMutation({
    mutationFn: () => publishLegalDocument(type, version.trim(), content),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["master", "legal", type] });
      setSaved(true);
      setErr(null);
      setTimeout(() => setSaved(false), 3000);
    },
    onError: (e: Error) => setErr(e.message),
  });

  return (
    <div className="min-h-screen bg-gray-50 text-gray-900">
      <div className="bg-white border-b border-gray-200 sticky top-0 z-30">
        <div className="max-w-7xl mx-auto px-6 h-14 flex items-center gap-4">
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5" style={{ color: "#7c5cf8" }} />
            <span className="font-black text-gray-900 text-sm">Master Admin</span>
          </div>
          <div className="flex-1" />
          <button
            onClick={() => { clearMasterToken(); window.location.href = "/master/login"; }}
            className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-800 transition"
          >
            <LogOut className="w-4 h-4" />
            Sair
          </button>
        </div>
      </div>

      <main className="max-w-2xl mx-auto px-6 py-6">
        <div className="mb-5">
          <Link to="/master" className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-800 transition mb-3">
            <ChevronLeft className="w-4 h-4" />
            Empresas
          </Link>
          <h1 className="text-xl font-black text-gray-900">Termos &amp; Privacidade</h1>
          <p className="text-sm text-gray-500 mt-1">
            Publicar uma versão nova desativa a anterior automaticamente. Histórico fica preservado.
          </p>
        </div>

        <div className="flex gap-2 mb-4">
          {TABS.map((t) => (
            <button
              key={t.key}
              onClick={() => setType(t.key)}
              className={`px-4 h-9 rounded-xl text-sm font-semibold transition ${
                type === t.key ? "text-white" : "text-gray-600 border border-gray-200 hover:bg-gray-100"
              }`}
              style={type === t.key ? { background: "linear-gradient(135deg, #7c5cf8, #6d4df2)" } : undefined}
            >
              {t.label}
            </button>
          ))}
        </div>

        {isLoading ? (
          <div className="text-center py-12 text-sm text-gray-400">Carregando…</div>
        ) : (
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 space-y-4">
            {active && (
              <p className="text-xs text-gray-500">
                Versão ativa: <strong>{active.version}</strong> — publicada em{" "}
                {new Date(active.publishedAtUtc).toLocaleString("pt-BR")}
              </p>
            )}

            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1">Nova versão (ex: 1.0)</label>
              <input
                value={version}
                onChange={(e) => setVersion(e.target.value)}
                placeholder="1.0"
                className="w-40 h-10 px-3 rounded-xl border border-gray-200 text-sm outline-none focus:ring-2 focus:ring-[#7c5cf8] transition"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1">Conteúdo</label>
              <textarea
                value={content}
                onChange={(e) => setContent(e.target.value)}
                rows={16}
                className="w-full px-3 py-2 rounded-xl border border-gray-200 text-sm outline-none focus:ring-2 focus:ring-[#7c5cf8] transition font-mono"
              />
            </div>

            {err && <p className="text-sm text-red-600">{err}</p>}
            {saved && <p className="text-sm text-green-600">✓ Publicado!</p>}

            <button
              onClick={() => publishMut.mutate()}
              disabled={publishMut.isPending || !version.trim() || !content.trim()}
              className="h-10 px-6 rounded-xl font-semibold text-sm text-white disabled:opacity-60 transition hover:brightness-110"
              style={{ background: "linear-gradient(135deg, #7c5cf8, #6d4df2)" }}
            >
              {publishMut.isPending ? "Publicando…" : "Publicar nova versão"}
            </button>

            {history && history.length > 0 && (
              <div className="pt-4 border-t border-gray-100">
                <p className="text-xs font-bold text-gray-500 uppercase tracking-wide mb-2">Histórico</p>
                <div className="space-y-1">
                  {history.map((h) => (
                    <div key={h.id} className="text-xs text-gray-500 flex items-center gap-2">
                      <span className={`px-1.5 py-0.5 rounded-full font-semibold ${h.isActive ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-500"}`}>
                        v{h.version}
                      </span>
                      {new Date(h.publishedAtUtc).toLocaleString("pt-BR")}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}
