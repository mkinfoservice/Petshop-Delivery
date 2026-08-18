import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { adminFetch } from "@/features/admin/auth/adminFetch";
import {
  Loader2, FileText, Filter, ChevronLeft, ChevronRight,
  AlertTriangle, RefreshCw, Send, Printer, Trash2, Ban,
} from "lucide-react";

// ── Helpers ───────────────────────────────────────────────────────────────────

function fmtDateTime(iso: string | null) {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("pt-BR", {
    day: "2-digit", month: "2-digit", year: "2-digit",
    hour: "2-digit", minute: "2-digit",
  });
}

function fmtAccessKey(key: string | null) {
  if (!key) return "—";
  return key.match(/.{1,4}/g)?.join(" ") ?? key;
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface FiscalDocumentItem {
  id: string;
  number: number;
  serie: number;
  accessKey: string | null;
  fiscalStatus: string;
  contingencyType: string;
  isContingency: boolean;
  saleOrderId: string | null;
  salePublicId: string | null;
  rejectCode: string | null;
  rejectMessage: string | null;
  transmissionAttempts: number;
  authorizationDateTimeUtc: string | null;
  lastAttemptAtUtc: string | null;
  createdAtUtc: string;
  cancelReason: string | null;
  cancelProtocol: string | null;
  cancelledAtUtc: string | null;
  hasXml: boolean;
}

interface FiscalDocumentsResponse {
  total: number;
  page: number;
  pageSize: number;
  items: FiscalDocumentItem[];
}

// ── API ───────────────────────────────────────────────────────────────────────

const API = import.meta.env.VITE_API_URL ?? "";

async function listFiscalDocuments(params: {
  page: number; pageSize: number;
  status?: string; contingency?: string; from?: string; to?: string;
}): Promise<FiscalDocumentsResponse> {
  const q = new URLSearchParams();
  q.set("page", String(params.page));
  q.set("pageSize", String(params.pageSize));
  if (params.status)      q.set("status", params.status);
  if (params.contingency) q.set("contingency", params.contingency);
  if (params.from)        q.set("from", params.from);
  if (params.to)          q.set("to", params.to);
  return adminFetch<FiscalDocumentsResponse>(`/admin/fiscal/documents?${q.toString()}`);
}

async function resetQueue() {
  return adminFetch<{ reset: number; message: string }>(
    "/admin/fiscal/debug/reset-queue?force=true",
    { method: "POST" }
  );
}

async function processQueue() {
  return adminFetch<{ jobId: string; message: string }>(
    "/admin/fiscal/debug/process-queue",
    { method: "POST" }
  );
}

async function cleanupContingency(keep: number) {
  return adminFetch<{ deletedDocuments: number; kept: number; message: string }>(
    `/admin/fiscal/debug/cleanup-contingency?keep=${keep}`,
    { method: "DELETE" }
  );
}

async function cancelNfce(saleOrderId: string, reason: string) {
  return adminFetch<{ status: string; cancelProtocol: string | null; cancelledAtUtc: string }>(
    `/admin/fiscal/sale/${saleOrderId}/cancel`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ reason }),
    }
  );
}

// ── Status badges ─────────────────────────────────────────────────────────────

const fiscalStatusLabel: Record<string, string> = {
  Authorized:  "Autorizada",
  Pending:     "Pendente",
  Rejected:    "Rejeitada",
  Contingency: "Contingência",
  Cancelled:   "Cancelada",
};

const fiscalStatusClass: Record<string, string> = {
  Authorized:  "bg-green-100 text-green-700",
  Pending:     "bg-yellow-100 text-yellow-800",
  Rejected:    "bg-red-100 text-red-700",
  Contingency: "bg-orange-100 text-orange-700",
  Cancelled:   "bg-gray-100 text-gray-500",
};

// ── Row expandido ──────────────────────────────────────────────────────────────

function ExpandedRow({ doc }: { doc: FiscalDocumentItem }) {
  return (
    <tr className="bg-amber-50/50">
      <td colSpan={9} className="px-6 py-3">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
          <div>
            <span className="text-xs text-gray-500 block mb-1">Chave de Acesso</span>
            <span className="font-mono text-xs text-gray-700 break-all">
              {fmtAccessKey(doc.accessKey)}
            </span>
          </div>
          {doc.rejectCode && (
            <div>
              <span className="text-xs text-gray-500 block mb-1">Rejeição SEFAZ</span>
              <span className="text-red-600 font-medium">
                [{doc.rejectCode}] {doc.rejectMessage}
              </span>
            </div>
          )}
          {doc.fiscalStatus === "Cancelled" && (
            <div>
              <span className="text-xs text-gray-500 block mb-1">Cancelamento</span>
              <span className="text-gray-700">
                {fmtDateTime(doc.cancelledAtUtc)} — protocolo {doc.cancelProtocol ?? "—"}
              </span>
              {doc.cancelReason && (
                <span className="block text-xs text-gray-500 mt-0.5">Motivo: {doc.cancelReason}</span>
              )}
            </div>
          )}
          <div className="flex gap-6 text-xs text-gray-600">
            <span>Tentativas: <strong>{doc.transmissionAttempts}</strong></span>
            <span>Última tentativa: <strong>{fmtDateTime(doc.lastAttemptAtUtc)}</strong></span>
            <span>XML: <strong>{doc.hasXml ? "Disponível" : "Ausente"}</strong></span>
          </div>
        </div>
      </td>
    </tr>
  );
}

// ── Componente principal ──────────────────────────────────────────────────────

export default function FiscalDocumentsPage() {
  const qc = useQueryClient();
  const [page, setPage]                   = useState(1);
  const [pageSize]                         = useState(50);
  const [showFilters, setShowFilters]     = useState(false);
  const [expandedId, setExpandedId]       = useState<string | null>(null);
  const [actionMsg, setActionMsg]         = useState<string | null>(null);
  const [filters, setFilters]             = useState({
    status: "", contingency: "", from: "", to: "",
  });

  const { data, isLoading, isError } = useQuery<FiscalDocumentsResponse>({
    queryKey: ["fiscal-documents", page, pageSize, filters],
    queryFn: () => listFiscalDocuments({
      page, pageSize,
      status:      filters.status || undefined,
      contingency: filters.contingency || undefined,
      from:        filters.from || undefined,
      to:          filters.to || undefined,
    }),
    staleTime: 30_000,
  });

  const cleanupMutation = useMutation({
    mutationFn: () => cleanupContingency(50),
    onSuccess: (res) => {
      setActionMsg(res.message);
      setTimeout(() => {
        setActionMsg(null);
        qc.invalidateQueries({ queryKey: ["fiscal-documents"] });
      }, 4000);
    },
    onError: () => setActionMsg("Erro ao limpar contingências."),
  });

  const transmitMutation = useMutation({
    mutationFn: async () => {
      const reset = await resetQueue();
      const job   = await processQueue();
      return { reset, job };
    },
    onSuccess: ({ reset }) => {
      setActionMsg(
        reset.reset > 0
          ? `${reset.reset} item(ns) resetado(s) e job enfileirado. Aguarde alguns segundos.`
          : "Nenhum item pendente. Job de transmissão disparado."
      );
      setTimeout(() => {
        setActionMsg(null);
        qc.invalidateQueries({ queryKey: ["fiscal-documents"] });
      }, 4000);
    },
    onError: () => setActionMsg("Erro ao disparar transmissão. Tente novamente."),
  });

  const cancelMutation = useMutation({
    mutationFn: ({ saleOrderId, reason }: { saleOrderId: string; reason: string }) =>
      cancelNfce(saleOrderId, reason),
    onSuccess: () => {
      setActionMsg("NFC-e cancelada com sucesso junto à SEFAZ.");
      setTimeout(() => setActionMsg(null), 4000);
      qc.invalidateQueries({ queryKey: ["fiscal-documents"] });
    },
    onError: (e: Error) => setActionMsg(`Falha ao cancelar: ${e.message}`),
  });

  function handleCancel(saleOrderId: string) {
    const reason = window.prompt(
      "Motivo do cancelamento (mínimo 15 caracteres — exigido pela SEFAZ):"
    );
    if (reason === null) return;
    const trimmed = reason.trim();
    if (trimmed.length < 15) {
      setActionMsg("Motivo precisa ter ao menos 15 caracteres.");
      return;
    }
    if (!confirm("Cancelar esta NFC-e junto à SEFAZ? Esta ação é irreversível.")) return;
    cancelMutation.mutate({ saleOrderId, reason: trimmed });
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.total / pageSize)) : 1;

  function updateFilter(key: keyof typeof filters, value: string) {
    setFilters(f => ({ ...f, [key]: value }));
    setPage(1);
  }

  const contingencyCount = data?.items.filter(d => d.isContingency).length ?? 0;
  const pendingCount     = data?.items.filter(d => d.fiscalStatus === "Pending").length ?? 0;
  const rejectedCount    = data?.items.filter(d => d.fiscalStatus === "Rejected").length ?? 0;

  function openDanfe(saleOrderId: string) {
    const token = localStorage.getItem("adminToken") ?? "";
    // Abre DANFE em nova aba — backend retorna HTML
    const url = `${API}/admin/fiscal/sale/${saleOrderId}/danfe`;
    const win  = window.open("about:blank", "_blank");
    if (!win) return;
    fetch(url, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => r.text())
      .then(html => { win.document.write(html); win.document.close(); });
  }

  return (
    <div className="p-4 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <FileText className="w-5 h-5 text-brand" />
          <h1 className="text-xl font-semibold text-gray-800">Documentos Fiscais (NFC-e)</h1>
          {data && (
            <span className="ml-2 text-sm text-gray-500">
              {data.total.toLocaleString("pt-BR")} documento(s)
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {/* Limpar contingências antigas */}
          <button
            onClick={() => {
              if (confirm("Apagar contingências antigas, mantendo as 50 mais recentes?"))
                cleanupMutation.mutate();
            }}
            disabled={cleanupMutation.isPending}
            title="Remove documentos em contingência antigos, mantém os 50 mais recentes"
            className="flex items-center gap-1.5 px-3 py-1.5 border border-red-200 text-red-600 text-sm rounded-lg hover:bg-red-50 disabled:opacity-60 transition"
          >
            {cleanupMutation.isPending
              ? <Loader2 className="w-4 h-4 animate-spin" />
              : <Trash2 className="w-4 h-4" />}
            Limpar contingências
          </button>
          {/* Botão transmitir */}
          <button
            onClick={() => transmitMutation.mutate()}
            disabled={transmitMutation.isPending}
            title="Reseta erros/contingências e transmite para SEFAZ agora"
            className="flex items-center gap-1.5 px-3 py-1.5 bg-brand text-white text-sm rounded-lg hover:brightness-110 disabled:opacity-60 transition"
          >
            {transmitMutation.isPending
              ? <Loader2 className="w-4 h-4 animate-spin" />
              : <Send className="w-4 h-4" />}
            Transmitir para SEFAZ
          </button>
          {/* Atualizar */}
          <button
            onClick={() => qc.invalidateQueries({ queryKey: ["fiscal-documents"] })}
            className="p-1.5 rounded-lg border hover:bg-gray-50 transition"
            title="Atualizar lista"
          >
            <RefreshCw className="w-4 h-4 text-gray-500" />
          </button>
          <button
            onClick={() => setShowFilters(v => !v)}
            className="flex items-center gap-1 text-sm text-gray-600 hover:text-brand transition"
          >
            <Filter className="w-4 h-4" />
            Filtros
          </button>
        </div>
      </div>

      {/* Feedback de ação */}
      {actionMsg && (
        <div className="mb-3 px-4 py-2 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-700">
          {actionMsg}
        </div>
      )}

      {/* Alertas rápidos */}
      {(contingencyCount > 0 || pendingCount > 0 || rejectedCount > 0) && (
        <div className="flex flex-wrap gap-2 mb-4">
          {contingencyCount > 0 && (
            <div className="flex items-center gap-1.5 px-3 py-2 bg-orange-50 border border-orange-200 rounded-lg text-sm text-orange-700">
              <AlertTriangle className="w-4 h-4" />
              {contingencyCount} em contingência (transmitir em até 168h)
            </div>
          )}
          {pendingCount > 0 && (
            <div className="flex items-center gap-1.5 px-3 py-2 bg-yellow-50 border border-yellow-200 rounded-lg text-sm text-yellow-700">
              <AlertTriangle className="w-4 h-4" />
              {pendingCount} pendente(s) de transmissão
            </div>
          )}
          {rejectedCount > 0 && (
            <div className="flex items-center gap-1.5 px-3 py-2 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
              <AlertTriangle className="w-4 h-4" />
              {rejectedCount} rejeitada(s) — verificar código de erro
            </div>
          )}
        </div>
      )}

      {/* Filtros */}
      {showFilters && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 p-4 bg-gray-50 rounded-xl mb-4 text-sm">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Status fiscal</label>
            <select
              value={filters.status}
              onChange={e => updateFilter("status", e.target.value)}
              className="w-full border rounded-lg px-2 py-1.5"
            >
              <option value="">Todos</option>
              <option value="Authorized">Autorizada</option>
              <option value="Pending">Pendente</option>
              <option value="Rejected">Rejeitada</option>
              <option value="Contingency">Contingência</option>
              <option value="Cancelled">Cancelada</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Contingência</label>
            <select
              value={filters.contingency}
              onChange={e => updateFilter("contingency", e.target.value)}
              className="w-full border rounded-lg px-2 py-1.5"
            >
              <option value="">Todas</option>
              <option value="None">Sem contingência</option>
              <option value="Offline">Offline (FS-DA)</option>
              <option value="SvCan">SVC-AN</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Data início</label>
            <input
              type="date" value={filters.from}
              onChange={e => updateFilter("from", e.target.value)}
              className="w-full border rounded-lg px-2 py-1.5"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Data fim</label>
            <input
              type="date" value={filters.to}
              onChange={e => updateFilter("to", e.target.value)}
              className="w-full border rounded-lg px-2 py-1.5"
            />
          </div>
        </div>
      )}

      {/* Tabela */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <Loader2 className="w-6 h-6 animate-spin text-brand" />
        </div>
      ) : isError ? (
        <div className="text-center py-12 text-red-500">Erro ao carregar documentos fiscais.</div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <FileText className="w-10 h-10 mx-auto mb-3 opacity-30" />
          <p>Nenhum documento fiscal encontrado.</p>
        </div>
      ) : (
        <>
          <div className="overflow-x-auto rounded-xl border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-600 text-xs uppercase tracking-wide">
                <tr>
                  <th className="px-4 py-3 text-left">Emissão</th>
                  <th className="px-4 py-3 text-left">Nº / Série</th>
                  <th className="px-4 py-3 text-left">Venda vinculada</th>
                  <th className="px-4 py-3 text-center">Status</th>
                  <th className="px-4 py-3 text-center">Contingência</th>
                  <th className="px-4 py-3 text-left">Autorização</th>
                  <th className="px-4 py-3 text-center">XML</th>
                  <th className="px-4 py-3 text-center">DANFE</th>
                  <th className="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {data.items.map(doc => (
                  <>
                    <tr key={doc.id} className="hover:bg-gray-50 transition">
                      <td className="px-4 py-3 text-gray-600 whitespace-nowrap">
                        {fmtDateTime(doc.createdAtUtc)}
                      </td>
                      <td className="px-4 py-3">
                        <span className="font-mono text-gray-800">{doc.number}</span>
                        <span className="text-xs text-gray-400 ml-1">/ {doc.serie}</span>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-gray-600">
                        {doc.salePublicId ?? (doc.saleOrderId ? doc.saleOrderId.slice(0, 8) + "…" : "—")}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${fiscalStatusClass[doc.fiscalStatus] ?? "bg-gray-100 text-gray-600"}`}>
                          {fiscalStatusLabel[doc.fiscalStatus] ?? doc.fiscalStatus}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {doc.isContingency ? (
                          <span className="flex items-center justify-center gap-1 text-orange-600 text-xs">
                            <AlertTriangle className="w-3 h-3" />
                            {doc.contingencyType}
                          </span>
                        ) : (
                          <span className="text-gray-400 text-xs">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-gray-600 text-xs whitespace-nowrap">
                        {fmtDateTime(doc.authorizationDateTimeUtc)}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {doc.hasXml ? (
                          <span className="text-green-600 text-xs font-medium">✓</span>
                        ) : (
                          <span className="text-gray-400 text-xs">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {doc.saleOrderId && (doc.fiscalStatus === "Authorized" || doc.hasXml) ? (
                          <button
                            onClick={() => openDanfe(doc.saleOrderId!)}
                            title="Abrir DANFE / comprovante"
                            className="inline-flex items-center gap-1 px-2 py-1 rounded text-xs bg-gray-100 hover:bg-brand hover:text-white transition"
                          >
                            <Printer className="w-3 h-3" />
                            DANFE
                          </button>
                        ) : (
                          <span className="text-gray-300 text-xs">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex items-center justify-end gap-2">
                          {doc.fiscalStatus === "Authorized" && doc.saleOrderId && (
                            <button
                              onClick={() => handleCancel(doc.saleOrderId!)}
                              disabled={cancelMutation.isPending}
                              title="Cancelar NFC-e junto à SEFAZ"
                              className="inline-flex items-center gap-1 px-2 py-1 rounded text-xs bg-red-50 text-red-600 hover:bg-red-100 disabled:opacity-50 transition"
                            >
                              <Ban className="w-3 h-3" />
                              Cancelar
                            </button>
                          )}
                          <button
                            onClick={() => setExpandedId(expandedId === doc.id ? null : doc.id)}
                            className="text-brand hover:underline text-xs"
                          >
                            {expandedId === doc.id ? "Fechar" : "Detalhes"}
                          </button>
                        </div>
                      </td>
                    </tr>
                    {expandedId === doc.id && <ExpandedRow key={`exp-${doc.id}`} doc={doc} />}
                  </>
                ))}
              </tbody>
            </table>
          </div>

          {/* Paginação */}
          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>
              {((page - 1) * pageSize) + 1}–{Math.min(page * pageSize, data.total)} de {data.total.toLocaleString("pt-BR")}
            </span>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="p-1 rounded hover:bg-gray-100 disabled:opacity-40"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <span>Página {page} de {totalPages}</span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="p-1 rounded hover:bg-gray-100 disabled:opacity-40"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
