import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";

const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5082";

type LegalDocumentDto = {
  documentType: string;
  version: string;
  content: string;
  publishedAtUtc: string;
};

async function fetchLegalDocument(type: "terms" | "privacy"): Promise<LegalDocumentDto> {
  const r = await fetch(`${API_URL}/public/legal/${type}`);
  if (!r.ok) throw new Error("Documento ainda não publicado.");
  return r.json();
}

export function LegalDocumentPage({ type, title }: { type: "terms" | "privacy"; title: string }) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["legal", type],
    queryFn: () => fetchLegalDocument(type),
  });

  return (
    <div className="min-h-dvh px-4 py-10" style={{ background: "var(--bg)" }}>
      <div className="max-w-2xl mx-auto rounded-3xl p-8" style={{ background: "var(--surface)" }}>
        <h1 className="text-2xl font-black mb-1" style={{ color: "var(--text)" }}>{title}</h1>

        {isLoading && (
          <p className="text-sm mt-4" style={{ color: "var(--text-muted)" }}>Carregando…</p>
        )}

        {isError && (
          <p className="text-sm mt-4" style={{ color: "var(--text-muted)" }}>
            Documento ainda não publicado.
          </p>
        )}

        {data && (
          <>
            <p className="text-xs mb-6" style={{ color: "var(--text-muted)" }}>
              Versão {data.version} — publicado em {new Date(data.publishedAtUtc).toLocaleDateString("pt-BR")}
            </p>
            <div className="whitespace-pre-wrap text-sm leading-relaxed" style={{ color: "var(--text)" }}>
              {data.content}
            </div>
          </>
        )}

        <Link to="/login" className="inline-block mt-8 text-sm font-semibold hover:underline" style={{ color: "var(--brand)" }}>
          ← Voltar
        </Link>
      </div>
    </div>
  );
}
