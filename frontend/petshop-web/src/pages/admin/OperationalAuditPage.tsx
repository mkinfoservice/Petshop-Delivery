import { useMemo, useState } from "react";
import { Copy, Eye, Filter, RotateCw, Search, X } from "lucide-react";
import { PageHeader } from "@/components/ui/PageHeader";
import {
  useOperationalAudit,
  useOperationalAuditDetail,
} from "@/features/admin/audit/queries";
import type {
  OperationalAuditFilters,
  OperationalAuditListItem,
} from "@/features/admin/audit/types";

const PAGE_SIZE = 25;

const ACTION_OPTIONS = [
  "",
  "order.status.update",
  "order.status.retrograde",
  "order.delivery.purge",
  "dav.create",
  "dav.update",
  "dav.convert",
  "dav.delete",
  "route.create",
  "route.start",
  "route.stop.delivered",
  "route.stop.failed",
  "route.delete",
  "deliverer.pin.update",
  "feature_flags.update",
  "storefront.branding.update",
  "storefront.slide.create",
  "storefront.slide.update",
  "storefront.slide.delete",
  "storefront.slide.reorder",
];

const ACTION_LABELS: Record<string, string> = {
  "order.status.update": "Pedido: status",
  "order.status.retrograde": "Pedido: retrocesso",
  "order.delivery.purge": "Pedidos: limpeza",
  "dav.create": "DAV: criado",
  "dav.update": "DAV: atualizado",
  "dav.convert": "DAV: convertido",
  "dav.delete": "DAV: removido",
  "route.create": "Rota: criada",
  "route.start": "Rota: iniciada",
  "route.stop.delivered": "Rota: entrega",
  "route.stop.failed": "Rota: falha",
  "route.delete": "Rota: removida",
  "deliverer.pin.update": "Entregador: PIN",
  "feature_flags.update": "Feature flags",
  "storefront.branding.update": "Loja: branding",
  "storefront.slide.create": "Banner: criado",
  "storefront.slide.update": "Banner: atualizado",
  "storefront.slide.delete": "Banner: removido",
  "storefront.slide.reorder": "Banner: ordem",
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatAction(action: string) {
  return ACTION_LABELS[action] ?? action;
}

function compactId(value: string | null) {
  if (!value) return "-";
  if (value.length <= 12) return value;
  return `${value.slice(0, 8)}...${value.slice(-4)}`;
}

function formatPayload(payloadJson: string | null) {
  if (!payloadJson) return "Sem payload.";

  try {
    return JSON.stringify(JSON.parse(payloadJson), null, 2);
  } catch {
    return payloadJson;
  }
}

function toApiDate(value: string) {
  return value ? new Date(value).toISOString() : undefined;
}

export default function OperationalAuditPage() {
  const [draft, setDraft] = useState({
    action: "",
    targetType: "",
    targetId: "",
    correlationId: "",
    from: "",
    to: "",
  });
  const [page, setPage] = useState(1);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [applied, setApplied] = useState(draft);

  const filters = useMemo<OperationalAuditFilters>(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      action: applied.action || undefined,
      targetType: applied.targetType.trim() || undefined,
      targetId: applied.targetId.trim() || undefined,
      correlationId: applied.correlationId.trim() || undefined,
      from: toApiDate(applied.from),
      to: toApiDate(applied.to),
    }),
    [applied, page],
  );

  const auditQuery = useOperationalAudit(filters);
  const detailQuery = useOperationalAuditDetail(selectedId);

  const total = auditQuery.data?.total ?? 0;
  const items = auditQuery.data?.items ?? [];
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  function applyFilters() {
    setPage(1);
    setSelectedId(null);
    setApplied(draft);
  }

  function clearFilters() {
    const empty = {
      action: "",
      targetType: "",
      targetId: "",
      correlationId: "",
      from: "",
      to: "",
    };
    setDraft(empty);
    setApplied(empty);
    setPage(1);
    setSelectedId(null);
  }

  async function copy(text: string | null) {
    if (!text) return;
    await navigator.clipboard?.writeText(text);
  }

  function selectRow(item: OperationalAuditListItem) {
    setSelectedId((current) => (current === item.id ? null : item.id));
  }

  return (
    <main className="px-4 py-5 sm:px-6 lg:px-8">
      <PageHeader
        title="Auditoria operacional"
        subtitle="Eventos por tenant"
        actions={
          <button
            type="button"
            onClick={() => auditQuery.refetch()}
            className="inline-flex h-10 items-center gap-2 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm font-semibold text-[var(--text)] hover:bg-[var(--surface-2)]"
          >
            <RotateCw size={16} className={auditQuery.isFetching ? "animate-spin" : ""} />
            Atualizar
          </button>
        }
      />

      <section className="mb-4 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-sm">
        <div className="mb-3 flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-[var(--text-muted)]">
          <Filter size={16} />
          Filtros
        </div>

        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-6">
          <label className="space-y-1 text-sm font-semibold text-[var(--text-muted)]">
            Ação
            <select
              value={draft.action}
              onChange={(e) => setDraft((v) => ({ ...v, action: e.target.value }))}
              className="h-11 w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--text)] outline-none focus:border-[var(--brand)]"
            >
              <option value="">Todas</option>
              {ACTION_OPTIONS.filter(Boolean).map((action) => (
                <option key={action} value={action}>
                  {formatAction(action)}
                </option>
              ))}
            </select>
          </label>

          <label className="space-y-1 text-sm font-semibold text-[var(--text-muted)]">
            Tipo
            <input
              value={draft.targetType}
              onChange={(e) => setDraft((v) => ({ ...v, targetType: e.target.value }))}
              className="h-11 w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--text)] outline-none focus:border-[var(--brand)]"
              placeholder="Order"
            />
          </label>

          <label className="space-y-1 text-sm font-semibold text-[var(--text-muted)]">
            ID alvo
            <input
              value={draft.targetId}
              onChange={(e) => setDraft((v) => ({ ...v, targetId: e.target.value }))}
              className="h-11 w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--text)] outline-none focus:border-[var(--brand)]"
              placeholder="PS-..."
            />
          </label>

          <label className="space-y-1 text-sm font-semibold text-[var(--text-muted)]">
            Correlation
            <input
              value={draft.correlationId}
              onChange={(e) => setDraft((v) => ({ ...v, correlationId: e.target.value }))}
              className="h-11 w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--text)] outline-none focus:border-[var(--brand)]"
              placeholder="trace"
            />
          </label>

          <label className="space-y-1 text-sm font-semibold text-[var(--text-muted)]">
            De
            <input
              type="datetime-local"
              value={draft.from}
              onChange={(e) => setDraft((v) => ({ ...v, from: e.target.value }))}
              className="h-11 w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--text)] outline-none focus:border-[var(--brand)]"
            />
          </label>

          <label className="space-y-1 text-sm font-semibold text-[var(--text-muted)]">
            Até
            <input
              type="datetime-local"
              value={draft.to}
              onChange={(e) => setDraft((v) => ({ ...v, to: e.target.value }))}
              className="h-11 w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--text)] outline-none focus:border-[var(--brand)]"
            />
          </label>
        </div>

        <div className="mt-4 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={applyFilters}
            className="inline-flex h-10 items-center gap-2 rounded-lg bg-[var(--brand)] px-4 text-sm font-bold text-white hover:brightness-95"
          >
            <Search size={16} />
            Buscar
          </button>
          <button
            type="button"
            onClick={clearFilters}
            className="inline-flex h-10 items-center gap-2 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-4 text-sm font-semibold text-[var(--text)] hover:bg-[var(--surface-2)]"
          >
            <X size={16} />
            Limpar
          </button>
        </div>
      </section>

      <section className="overflow-hidden rounded-xl border border-[var(--border)] bg-[var(--surface)] shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[var(--border)] px-4 py-3">
          <div>
            <p className="text-sm font-bold text-[var(--text)]">Eventos</p>
            <p className="text-xs font-medium text-[var(--text-muted)]">
              {total} registro(s)
            </p>
          </div>
          <div className="flex items-center gap-2 text-sm font-semibold text-[var(--text-muted)]">
            Página {page} de {totalPages}
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-[var(--border)] text-left text-sm">
            <thead className="bg-[var(--surface-2)] text-xs uppercase tracking-wide text-[var(--text-muted)]">
              <tr>
                <th className="px-4 py-3">Data</th>
                <th className="px-4 py-3">Ação</th>
                <th className="px-4 py-3">Alvo</th>
                <th className="px-4 py-3">Ator</th>
                <th className="px-4 py-3">Correlation</th>
                <th className="px-4 py-3 text-right">Detalhe</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--border)]">
              {auditQuery.isLoading ? (
                <tr>
                  <td className="px-4 py-8 text-center text-[var(--text-muted)]" colSpan={6}>
                    Carregando...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-4 py-8 text-center text-[var(--text-muted)]" colSpan={6}>
                    Nenhum evento encontrado.
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr
                    key={item.id}
                    className="cursor-pointer text-[var(--text)] hover:bg-[var(--surface-2)]"
                    onClick={() => selectRow(item)}
                  >
                    <td className="whitespace-nowrap px-4 py-3 font-medium">
                      {formatDate(item.createdAtUtc)}
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-bold">{formatAction(item.action)}</div>
                      <div className="text-xs text-[var(--text-muted)]">{item.action}</div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-semibold">{item.targetName || item.targetType}</div>
                      <div className="text-xs text-[var(--text-muted)]">
                        {item.targetType} · {compactId(item.targetId)}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-semibold">{item.actorUsername || "-"}</div>
                      <div className="text-xs text-[var(--text-muted)]">{item.actorRole || "-"}</div>
                    </td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          copy(item.correlationId);
                        }}
                        className="inline-flex items-center gap-2 rounded-md border border-transparent px-2 py-1 font-mono text-xs text-[var(--text-muted)] hover:border-[var(--border)] hover:text-[var(--text)]"
                      >
                        <Copy size={14} />
                        {compactId(item.correlationId)}
                      </button>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-[var(--border)] text-[var(--text)] hover:bg-[var(--surface)]"
                        aria-label="Ver detalhe"
                      >
                        <Eye size={16} />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--border)] px-4 py-3">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((value) => Math.max(1, value - 1))}
            className="h-10 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-4 text-sm font-semibold text-[var(--text)] disabled:cursor-not-allowed disabled:opacity-45"
          >
            Anterior
          </button>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((value) => Math.min(totalPages, value + 1))}
            className="h-10 rounded-lg border border-[var(--border)] bg-[var(--surface)] px-4 text-sm font-semibold text-[var(--text)] disabled:cursor-not-allowed disabled:opacity-45"
          >
            Próxima
          </button>
        </div>
      </section>

      {selectedId && (
        <section className="mt-4 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-sm">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div>
              <p className="text-sm font-bold text-[var(--text)]">Detalhe do evento</p>
              <p className="font-mono text-xs text-[var(--text-muted)]">{selectedId}</p>
            </div>
            <button
              type="button"
              onClick={() => setSelectedId(null)}
              className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-[var(--border)] text-[var(--text)] hover:bg-[var(--surface-2)]"
              aria-label="Fechar detalhe"
            >
              <X size={16} />
            </button>
          </div>

          {detailQuery.isLoading ? (
            <p className="py-6 text-sm text-[var(--text-muted)]">Carregando detalhe...</p>
          ) : detailQuery.data ? (
            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,2fr)]">
              <dl className="grid gap-3 text-sm">
                <div>
                  <dt className="font-semibold text-[var(--text-muted)]">Ação</dt>
                  <dd className="font-bold text-[var(--text)]">{formatAction(detailQuery.data.action)}</dd>
                </div>
                <div>
                  <dt className="font-semibold text-[var(--text-muted)]">Alvo</dt>
                  <dd className="text-[var(--text)]">
                    {detailQuery.data.targetName || detailQuery.data.targetType}
                  </dd>
                </div>
                <div>
                  <dt className="font-semibold text-[var(--text-muted)]">Ator</dt>
                  <dd className="text-[var(--text)]">
                    {detailQuery.data.actorUsername || "-"} · {detailQuery.data.actorRole || "-"}
                  </dd>
                </div>
                <div>
                  <dt className="font-semibold text-[var(--text-muted)]">Correlation</dt>
                  <dd className="font-mono text-xs text-[var(--text)]">
                    {detailQuery.data.correlationId || "-"}
                  </dd>
                </div>
              </dl>

              <pre className="max-h-[420px] overflow-auto rounded-lg border border-[var(--border)] bg-[var(--surface-2)] p-4 text-xs leading-5 text-[var(--text)]">
                {formatPayload(detailQuery.data.payloadJson)}
              </pre>
            </div>
          ) : (
            <p className="py-6 text-sm text-[var(--text-muted)]">
              Não foi possível carregar o detalhe.
            </p>
          )}
        </section>
      )}
    </main>
  );
}
