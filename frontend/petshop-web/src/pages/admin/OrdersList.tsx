import { useState, useEffect, useMemo, type ReactNode } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useDeleteAllDeliveries, useDeleteFinalizedDeliveries, useOrders } from "@/features/admin/orders/queries";
import { type OrderStatus } from "@/features/admin/orders/status";
import { type OrderListItem } from "@/features/admin/orders/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import {
  Plus, ShoppingBag, Search, Trash2, Monitor, Coffee,
  ChevronRight, Inbox, Flame, CheckCircle2, Truck, PackageCheck, XCircle, AlertCircle,
} from "lucide-react";
import { useCurrentUser } from "@/hooks/useCurrentUser";
import { useQuery } from "@tanstack/react-query";
import { adminFetch } from "@/features/admin/auth/adminFetch";
import { fetchTenantInfo, resolveTenantFromHost } from "@/utils/tenant";

// ── Formatters ────────────────────────────────────────────────────────────────

function formatBRL(cents: number) {
  return (cents / 100).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function formatDate(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "—";
  const now = new Date();
  const isToday = d.toDateString() === now.toDateString();
  const yesterday = new Date(now);
  yesterday.setDate(yesterday.getDate() - 1);
  const isYesterday = d.toDateString() === yesterday.toDateString();
  const time = d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
  if (isToday) return `hoje, ${time}`;
  if (isYesterday) return `ontem, ${time}`;
  return `${d.toLocaleDateString("pt-BR", { day: "2-digit", month: "short" })}, ${time}`;
}

// ── PDV types & query ─────────────────────────────────────────────────────────

type PdvSaleItem = {
  id: string;
  publicId: string;
  customerName: string;
  customerPhone: string | null;
  status: string;
  totalCents: number;
  createdAtUtc: string;
  completedAtUtc: string | null;
  fromDav: boolean;
};

type ListPdvSalesResponse = {
  page: number;
  pageSize: number;
  total: number;
  items: PdvSaleItem[];
};

function fetchPdvSales(page: number, pageSize: number, status?: string, search?: string) {
  const params = new URLSearchParams();
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  if (status) params.set("status", status);
  if (search) params.set("search", search);
  return adminFetch<ListPdvSalesResponse>(`/pdv/sales?${params.toString()}`);
}

function usePdvSales(page: number, pageSize: number, status?: string, search?: string) {
  return useQuery({
    queryKey: ["pdv-sales", page, pageSize, status ?? "", search ?? ""],
    queryFn: () => fetchPdvSales(page, pageSize, status, search),
  });
}

// ── Status system ─────────────────────────────────────────────────────────────

type StatusCfg = { label: string; icon: typeof Inbox; classes: string };

const STATUS_CONFIG: Record<OrderStatus, StatusCfg> = {
  RECEBIDO:            { label: "Recebido",       icon: Inbox,        classes: "bg-blue-500/15 text-blue-400 border-blue-500/25" },
  EM_PREPARO:          { label: "Em preparo",      icon: Flame,        classes: "bg-amber-500/15 text-amber-400 border-amber-500/25" },
  PRONTO_PARA_ENTREGA: { label: "Pronto",          icon: CheckCircle2, classes: "bg-emerald-500/15 text-emerald-400 border-emerald-500/25" },
  SAIU_PARA_ENTREGA:   { label: "Saiu p/ entrega", icon: Truck,        classes: "bg-purple-500/15 text-purple-400 border-purple-500/25" },
  ENTREGUE:            { label: "Entregue",        icon: PackageCheck, classes: "bg-emerald-500/15 text-emerald-400 border-emerald-500/25" },
  CANCELADO:           { label: "Cancelado",       icon: XCircle,      classes: "bg-red-500/15 text-red-400 border-red-500/25" },
};

const PDV_STATUS_CONFIG: Record<string, StatusCfg> = {
  Open:      { label: "Aberta",    icon: ShoppingBag,  classes: "bg-amber-500/15 text-amber-400 border-amber-500/25" },
  Completed: { label: "Paga",      icon: PackageCheck, classes: "bg-emerald-500/15 text-emerald-400 border-emerald-500/25" },
  Cancelled: { label: "Cancelada", icon: XCircle,      classes: "bg-red-500/15 text-red-400 border-red-500/25" },
};

// ── Filter options ────────────────────────────────────────────────────────────

const ORDER_STATUS_FILTERS: { value: OrderStatus | ""; label: string }[] = [
  { value: "", label: "Todos" },
  { value: "RECEBIDO", label: "Recebido" },
  { value: "EM_PREPARO", label: "Em preparo" },
  { value: "PRONTO_PARA_ENTREGA", label: "Pronto" },
  { value: "SAIU_PARA_ENTREGA", label: "Saiu p/ entrega" },
  { value: "ENTREGUE", label: "Entregue" },
  { value: "CANCELADO", label: "Cancelado" },
];

const PDV_STATUS_FILTERS = [
  { value: "", label: "Todos" },
  { value: "Open", label: "Aberta" },
  { value: "Completed", label: "Paga" },
  { value: "Cancelled", label: "Cancelada" },
];

// ── Loading skeleton ──────────────────────────────────────────────────────────

function ListSkeleton() {
  return (
    <>
      {Array.from({ length: 8 }, (_, i) => (
        <div
          key={i}
          className="flex animate-pulse items-center gap-4 px-5 py-4"
          style={{ borderBottom: "1px solid var(--border)" }}
        >
          <div className="flex-1 space-y-2">
            <div className="h-3.5 w-20 rounded-full" style={{ backgroundColor: "var(--surface-2)" }} />
            <div className="h-3 w-36 rounded-full" style={{ backgroundColor: "var(--surface-2)" }} />
          </div>
          <div className="hidden h-6 w-24 rounded-full sm:block" style={{ backgroundColor: "var(--surface-2)" }} />
          <div className="space-y-2 text-right">
            <div className="ml-auto h-3.5 w-16 rounded-full" style={{ backgroundColor: "var(--surface-2)" }} />
            <div className="ml-auto h-3 w-24 rounded-full" style={{ backgroundColor: "var(--surface-2)" }} />
          </div>
          <div className="h-4 w-4 shrink-0 rounded" style={{ backgroundColor: "var(--surface-2)" }} />
        </div>
      ))}
    </>
  );
}

// ── Error state ───────────────────────────────────────────────────────────────

function ErrorState({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-14 text-center">
      <div
        className="mb-4 flex h-12 w-12 items-center justify-center rounded-full"
        style={{ backgroundColor: "color-mix(in srgb, #ef4444 12%, transparent)" }}
      >
        <AlertCircle size={22} className="text-red-400" />
      </div>
      <p className="text-sm font-semibold" style={{ color: "var(--text)" }}>{message}</p>
      <p className="mt-1 text-xs" style={{ color: "var(--text-muted)" }}>Tente recarregar a página</p>
    </div>
  );
}

// ── Status pills ──────────────────────────────────────────────────────────────

function StatusPills({
  options,
  value,
  onChange,
  configMap,
}: {
  options: { value: string; label: string }[];
  value: string;
  onChange: (v: string) => void;
  configMap?: Record<string, StatusCfg>;
}) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {options.map((o) => {
        const isActive = value === o.value;
        const cfg = o.value && configMap ? configMap[o.value] : undefined;
        const Icon = cfg?.icon;

        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className="inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-semibold transition-all"
            style={
              isActive
                ? {
                    background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))",
                    color: "#fff",
                    borderColor: "transparent",
                  }
                : {
                    backgroundColor: "var(--surface-2)",
                    borderColor: "var(--border)",
                    color: "var(--text-muted)",
                  }
            }
          >
            {Icon && <Icon size={10} />}
            {o.label}
          </button>
        );
      })}
    </div>
  );
}

// ── Order row ─────────────────────────────────────────────────────────────────

function OrderRow({ item, onClick }: { item: OrderListItem; onClick: () => void }) {
  const cfg = STATUS_CONFIG[item.status as OrderStatus];
  const Icon = cfg?.icon ?? CheckCircle2;

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => e.key === "Enter" && onClick()}
      className="group flex cursor-pointer items-center gap-4 px-5 py-4 outline-none transition-colors"
      style={{ borderBottom: "1px solid var(--border)" }}
      onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = "color-mix(in srgb, var(--brand) 6%, transparent)")}
      onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = "")}
      onFocus={(e) => (e.currentTarget.style.backgroundColor = "color-mix(in srgb, var(--brand) 6%, transparent)")}
      onBlur={(e) => (e.currentTarget.style.backgroundColor = "")}
    >
      {/* Left: order number + customer */}
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="font-bold text-sm" style={{ color: "var(--text)" }}>
            {item.orderNumber}
          </span>
          {item.isTableOrder && (
            <span
              className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-bold"
              style={{
                backgroundColor: "color-mix(in srgb, var(--brand-accent) 14%, transparent)",
                color: "var(--brand-accent)",
              }}
            >
              <Coffee size={9} />
              Mesa {item.tableNumber ?? "—"}{item.tableName ? ` · ${item.tableName}` : ""}
            </span>
          )}
          {/* Status badge — visible only on mobile */}
          {cfg && (
            <span className={`sm:hidden inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-bold ${cfg.classes}`}>
              <Icon size={9} />
              {cfg.label}
            </span>
          )}
        </div>
        <p className="mt-1 truncate text-sm" style={{ color: "var(--text-muted)" }}>
          {item.customerName}
        </p>
      </div>

      {/* Center: status badge — hidden on mobile */}
      {cfg && (
        <span className={`hidden shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-bold sm:inline-flex ${cfg.classes}`}>
          <Icon size={11} />
          {cfg.label}
        </span>
      )}

      {/* Right: value + date */}
      <div className="shrink-0 text-right">
        <p className="font-bold text-sm" style={{ color: "var(--text)" }}>
          {formatBRL(item.totalCents)}
        </p>
        <p className="mt-0.5 text-xs" style={{ color: "var(--text-muted)" }}>
          {formatDate(item.createdAtUtc)}
        </p>
      </div>

      <ChevronRight
        size={15}
        className="shrink-0 opacity-40 transition-all group-hover:translate-x-0.5 group-hover:opacity-70"
        style={{ color: "var(--text-muted)" }}
      />
    </div>
  );
}

// ── PDV row ───────────────────────────────────────────────────────────────────

function PdvRow({ item }: { item: PdvSaleItem }) {
  const cfg = PDV_STATUS_CONFIG[item.status] ?? {
    label: item.status,
    icon: ShoppingBag,
    classes: "border-[var(--border)] bg-[var(--surface-2)] text-[var(--text-muted)]",
  };
  const Icon = cfg.icon;

  return (
    <div
      className="flex items-center gap-4 px-5 py-4"
      style={{ borderBottom: "1px solid var(--border)" }}
    >
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="font-bold text-sm" style={{ color: "var(--text)" }}>
            {item.publicId}
          </span>
          {item.fromDav && (
            <span
              className="inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold"
              style={{
                backgroundColor: "color-mix(in srgb, var(--brand-accent) 14%, transparent)",
                color: "var(--brand-accent)",
              }}
            >
              DAV
            </span>
          )}
          <span className={`sm:hidden inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-bold ${cfg.classes}`}>
            <Icon size={9} />
            {cfg.label}
          </span>
        </div>
        <p className="mt-1 truncate text-sm" style={{ color: "var(--text-muted)" }}>
          {item.customerName || "—"}
        </p>
      </div>

      <span className={`hidden shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-bold sm:inline-flex ${cfg.classes}`}>
        <Icon size={11} />
        {cfg.label}
      </span>

      <div className="shrink-0 text-right">
        <p className="font-bold text-sm" style={{ color: "var(--text)" }}>
          {formatBRL(item.totalCents)}
        </p>
        <p className="mt-0.5 text-xs" style={{ color: "var(--text-muted)" }}>
          {formatDate(item.createdAtUtc)}
        </p>
      </div>
    </div>
  );
}

// ── List panel wrapper ────────────────────────────────────────────────────────

function ListPanel({
  label,
  page,
  totalPages,
  isFetching,
  children,
}: {
  label: string;
  page: number;
  totalPages: number;
  isFetching: boolean;
  children: ReactNode;
}) {
  return (
    <div className="mb-4 overflow-hidden rounded-2xl border" style={{ borderColor: "var(--border)" }}>
      <div
        className="flex items-center justify-between border-b px-5 py-3"
        style={{ borderColor: "var(--border)", backgroundColor: "var(--surface-2)" }}
      >
        <span className="text-xs font-semibold" style={{ color: "var(--text-muted)" }}>
          {isFetching ? "Atualizando..." : label}
        </span>
        <span className="text-xs" style={{ color: "var(--text-muted)" }}>
          Pág. {page} de {totalPages}
        </span>
      </div>
      <div style={{ backgroundColor: "var(--surface)" }}>{children}</div>
    </div>
  );
}

// ── Main component ────────────────────────────────────────────────────────────

export default function OrdersList() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { role } = useCurrentUser();

  const tenantSlug = resolveTenantFromHost();
  const tenantQuery = useQuery({
    queryKey: ["tenant"],
    queryFn: fetchTenantInfo,
    enabled: !!tenantSlug,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
  const ownDelivery = (tenantQuery.data?.features?.["own_delivery"] ?? true) === true;

  const [tab, setTab] = useState<"orders" | "pdv">("orders");

  // orders state
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<OrderStatus | "">((searchParams.get("status") as OrderStatus) || "");
  const [search, setSearch] = useState("");
  const [searchInput, setSearchInput] = useState("");

  // pdv state
  const [pdvPage, setPdvPage] = useState(1);
  const [pdvStatus, setPdvStatus] = useState("");
  const [pdvSearch, setPdvSearch] = useState("");
  const [pdvSearchInput, setPdvSearchInput] = useState("");

  // search debounce
  useEffect(() => {
    const t = setTimeout(() => { setPage(1); setSearch(searchInput); }, 300);
    return () => clearTimeout(t);
  }, [searchInput]);

  useEffect(() => {
    const t = setTimeout(() => { setPdvPage(1); setPdvSearch(pdvSearchInput); }, 300);
    return () => clearTimeout(t);
  }, [pdvSearchInput]);

  const ordersQuery = useOrders(page, 20, status || undefined, search || undefined);
  const pdvQuery = usePdvSales(pdvPage, 20, pdvStatus || undefined, pdvSearch || undefined);
  const deleteAllDeliveriesMut = useDeleteAllDeliveries();
  const deleteFinalizedDeliveriesMut = useDeleteFinalizedDeliveries();

  const totalPages = useMemo(() => {
    if (!ordersQuery.data) return 1;
    return Math.max(1, Math.ceil(ordersQuery.data.total / ordersQuery.data.pageSize));
  }, [ordersQuery.data]);

  const pdvTotalPages = useMemo(() => {
    if (!pdvQuery.data) return 1;
    return Math.max(1, Math.ceil(pdvQuery.data.total / pdvQuery.data.pageSize));
  }, [pdvQuery.data]);

  const items = ordersQuery.data?.items ?? [];
  const total = ordersQuery.data?.total ?? 0;
  const pdvItems = pdvQuery.data?.items ?? [];
  const pdvTotal = pdvQuery.data?.total ?? 0;

  const orderFilters = useMemo(
    () => ORDER_STATUS_FILTERS.filter((o) => ownDelivery || o.value !== "SAIU_PARA_ENTREGA"),
    [ownDelivery],
  );

  return (
    <div style={{ backgroundColor: "var(--bg)" }}>
      <div className="mx-auto max-w-[1400px] px-4 pb-12 pt-6">
        <PageHeader
          title="Pedidos"
          subtitle={
            tab === "orders"
              ? ordersQuery.isLoading ? "Carregando..." : `${total} pedido${total !== 1 ? "s" : ""}`
              : pdvQuery.isLoading ? "Carregando..." : `${pdvTotal} venda${pdvTotal !== 1 ? "s" : ""} no PDV`
          }
          actions={
            <div className="flex items-center gap-2">
              {role === "admin" && tab === "orders" && (
                <>
                  <button
                    type="button"
                    onClick={async () => {
                      if (!confirm("Apagar entregas encerradas (ENTREGUE e CANCELADO)?")) return;
                      try {
                        const res = await deleteFinalizedDeliveriesMut.mutateAsync();
                        alert(`Pedidos apagados: ${res.deletedOrders}`);
                      } catch (err) {
                        alert(err instanceof Error ? err.message : "Erro ao apagar entregas encerradas.");
                      }
                    }}
                    disabled={deleteFinalizedDeliveriesMut.isPending}
                    className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-semibold text-white transition-all hover:opacity-90 active:scale-95 disabled:opacity-60"
                    style={{ background: "linear-gradient(135deg, #b45309 0%, #f59e0b 100%)" }}
                  >
                    <Trash2 size={15} />
                    {deleteFinalizedDeliveriesMut.isPending ? "Apagando..." : "Apagar encerradas"}
                  </button>
                  <button
                    type="button"
                    onClick={async () => {
                      if (!confirm("Apagar TODOS os registros de entregas/pedidos desta empresa? Esta ação não pode ser desfeita.")) return;
                      try {
                        const res = await deleteAllDeliveriesMut.mutateAsync();
                        alert(`Pedidos apagados: ${res.deletedOrders}`);
                      } catch (err) {
                        alert(err instanceof Error ? err.message : "Erro ao apagar registros.");
                      }
                    }}
                    disabled={deleteAllDeliveriesMut.isPending}
                    className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-semibold text-white transition-all hover:opacity-90 active:scale-95 disabled:opacity-60"
                    style={{ background: "linear-gradient(135deg, #b42318 0%, #ef4444 100%)" }}
                  >
                    <Trash2 size={15} />
                    {deleteAllDeliveriesMut.isPending ? "Apagando..." : "Apagar tudo"}
                  </button>
                </>
              )}
              {tab === "orders" && ownDelivery && (
                <button
                  type="button"
                  onClick={() => navigate("/app/logistica/rotas/planner")}
                  className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-semibold text-white transition-all hover:opacity-90 active:scale-95"
                  style={{ background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))" }}
                >
                  <Plus size={15} />
                  Criar rota
                </button>
              )}
            </div>
          }
        />

        {/* Tab switcher */}
        <div
          className="mb-5 flex w-fit gap-1 rounded-2xl p-1"
          style={{ background: "var(--surface-2)" }}
        >
          <button
            type="button"
            onClick={() => setTab("orders")}
            className="flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition-all"
            style={
              tab === "orders"
                ? { background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))", color: "#fff" }
                : { color: "var(--text-muted)" }
            }
          >
            <ShoppingBag size={14} />
            Delivery / Balcão
          </button>
          <button
            type="button"
            onClick={() => setTab("pdv")}
            className="flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition-all"
            style={
              tab === "pdv"
                ? { background: "linear-gradient(135deg, var(--brand), color-mix(in srgb, var(--brand) 72%, #000))", color: "#fff" }
                : { color: "var(--text-muted)" }
            }
          >
            <Monitor size={14} />
            Frente de Caixa
          </button>
        </div>

        {/* Filter toolbar */}
        <div
          className="mb-4 space-y-3 rounded-2xl border p-4"
          style={{ backgroundColor: "var(--surface)", borderColor: "var(--border)" }}
        >
          <div className="relative">
            <Search
              size={15}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2"
              style={{ color: "var(--text-muted)" }}
            />
            <input
              className="h-10 w-full rounded-xl border pl-9 pr-3.5 text-sm outline-none transition-all focus:ring-2"
              style={{
                backgroundColor: "var(--surface-2)",
                borderColor: "var(--border)",
                color: "var(--text)",
                ["--tw-ring-color" as string]: "color-mix(in srgb, var(--brand-accent) 40%, transparent)",
              }}
              placeholder={tab === "orders" ? "Buscar por número do pedido..." : "Buscar por número ou cliente..."}
              value={tab === "orders" ? searchInput : pdvSearchInput}
              onChange={(e) =>
                tab === "orders" ? setSearchInput(e.target.value) : setPdvSearchInput(e.target.value)
              }
            />
          </div>

          <StatusPills
            options={tab === "orders" ? orderFilters : PDV_STATUS_FILTERS}
            value={tab === "orders" ? status : pdvStatus}
            onChange={(v) => {
              if (tab === "orders") { setPage(1); setStatus(v as OrderStatus | ""); }
              else { setPdvPage(1); setPdvStatus(v); }
            }}
            configMap={tab === "orders" ? STATUS_CONFIG : PDV_STATUS_CONFIG}
          />
        </div>

        {/* Orders list */}
        {tab === "orders" && (
          <>
            <ListPanel
              label={`${total} pedido${total !== 1 ? "s" : ""} encontrado${total !== 1 ? "s" : ""}`}
              page={page}
              totalPages={totalPages}
              isFetching={ordersQuery.isFetching}
            >
              {ordersQuery.isError && <ErrorState message="Erro ao carregar pedidos." />}
              {ordersQuery.isLoading && <ListSkeleton />}
              {!ordersQuery.isLoading && !ordersQuery.isError && items.length === 0 && (
                <EmptyState
                  icon={ShoppingBag}
                  title="Nenhum pedido encontrado"
                  description={search || status ? "Tente ajustar os filtros." : "Os pedidos do catálogo aparecerão aqui."}
                />
              )}
              {items.map((o) => (
                <OrderRow
                  key={o.id}
                  item={o}
                  onClick={() => navigate(`/app/pedidos/${o.orderNumber}`)}
                />
              ))}
            </ListPanel>

            <Pagination
              page={page}
              totalPages={totalPages}
              total={total}
              onPrev={() => setPage((p) => Math.max(1, p - 1))}
              onNext={() => setPage((p) => Math.min(totalPages, p + 1))}
            />
          </>
        )}

        {/* PDV list */}
        {tab === "pdv" && (
          <>
            <ListPanel
              label={`${pdvTotal} venda${pdvTotal !== 1 ? "s" : ""} no PDV`}
              page={pdvPage}
              totalPages={pdvTotalPages}
              isFetching={pdvQuery.isFetching}
            >
              {pdvQuery.isError && <ErrorState message="Erro ao carregar vendas do PDV." />}
              {pdvQuery.isLoading && <ListSkeleton />}
              {!pdvQuery.isLoading && !pdvQuery.isError && pdvItems.length === 0 && (
                <EmptyState
                  icon={Monitor}
                  title="Nenhuma venda no PDV"
                  description={
                    pdvSearch || pdvStatus
                      ? "Tente ajustar os filtros."
                      : "As vendas realizadas no Frente de Caixa aparecerão aqui."
                  }
                />
              )}
              {pdvItems.map((o) => (
                <PdvRow key={o.id} item={o} />
              ))}
            </ListPanel>

            <Pagination
              page={pdvPage}
              totalPages={pdvTotalPages}
              total={pdvTotal}
              onPrev={() => setPdvPage((p) => Math.max(1, p - 1))}
              onNext={() => setPdvPage((p) => Math.min(pdvTotalPages, p + 1))}
            />
          </>
        )}
      </div>
    </div>
  );
}
