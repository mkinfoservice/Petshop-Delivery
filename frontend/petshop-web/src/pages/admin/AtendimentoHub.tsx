import { Link } from "react-router-dom";
import { PageHeader } from "@/components/ui/PageHeader";
import { Phone, Users, UserPlus, ClipboardList, ArrowRight } from "lucide-react";

type HubItem = {
  icon: React.ElementType;
  iconColor: string;
  iconBg: string;
  label: string;
  description: string;
  to: string;
  highlight?: boolean;
};

const items: HubItem[] = [
  {
    icon: Phone,
    iconColor: "#ffffff",
    iconBg: "rgba(255,255,255,0.2)",
    label: "Montar pedido",
    description: "Atendimento por telefone - busca cliente, monta carrinho e confirma.",
    to: "/app/atendimento/pedido",
    highlight: true,
  },
  {
    icon: Users,
    iconColor: "var(--brand-2)",
    iconBg: "color-mix(in srgb, var(--brand-2) 14%, transparent)",
    label: "Clientes",
    description: "Lista e busca de clientes cadastrados.",
    to: "/app/atendimento/clientes",
  },
  {
    icon: UserPlus,
    iconColor: "#10b981",
    iconBg: "rgba(16,185,129,0.12)",
    label: "Novo cliente",
    description: "Cadastrar um novo cliente na base.",
    to: "/app/atendimento/clientes/novo",
  },
  {
    icon: ClipboardList,
    iconColor: "var(--brand-2)",
    iconBg: "color-mix(in srgb, var(--brand-2) 14%, transparent)",
    label: "Todos os pedidos",
    description: "Histórico completo de pedidos da loja.",
    to: "/app/pedidos",
  },
];

function HighlightCard({ item }: { item: HubItem }) {
  const Icon = item.icon;
  return (
    <Link
      to={item.to}
      className="group flex items-center gap-5 rounded-2xl p-6 transition-all active:scale-[0.99] hover:-translate-y-0.5"
      style={{
        background: `linear-gradient(135deg, var(--brand) 0%, color-mix(in srgb, var(--brand) 72%, #000) 100%)`,
        boxShadow: "0 4px 24px color-mix(in srgb, var(--brand) 35%, transparent)",
        textDecoration: "none",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLElement).style.boxShadow = "0 8px 32px color-mix(in srgb, var(--brand) 50%, transparent)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLElement).style.boxShadow = "0 4px 24px color-mix(in srgb, var(--brand) 35%, transparent)";
      }}
    >
      <div className="w-14 h-14 rounded-2xl flex items-center justify-center shrink-0"
        style={{ background: "rgba(255,255,255,0.15)" }}>
        <Icon size={28} style={{ color: "#fff" }} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-lg font-bold text-white">{item.label}</p>
        <p className="text-sm mt-0.5" style={{ color: "rgba(255,255,255,0.65)" }}>{item.description}</p>
      </div>
      <ArrowRight
        size={20}
        className="shrink-0 transition-transform group-hover:translate-x-1"
        style={{ color: "rgba(255,255,255,0.6)" }}
      />
    </Link>
  );
}

function ActionCard({ item }: { item: HubItem }) {
  const Icon = item.icon;
  return (
    <Link
      to={item.to}
      className="group flex items-center gap-4 rounded-2xl border p-5 transition-all hover:-translate-y-0.5 active:scale-[0.99]"
      style={{
        backgroundColor: "var(--surface)",
        borderColor: "var(--border)",
        textDecoration: "none",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLElement).style.borderColor = "color-mix(in srgb, var(--brand-accent) 35%, transparent)";
        (e.currentTarget as HTMLElement).style.boxShadow = "0 6px 20px color-mix(in srgb, var(--brand-accent) 10%, transparent)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLElement).style.borderColor = "var(--border)";
        (e.currentTarget as HTMLElement).style.boxShadow = "none";
      }}
    >
      <div
        className="w-11 h-11 rounded-xl flex items-center justify-center shrink-0"
        style={{ backgroundColor: item.iconBg }}
      >
        <Icon size={22} style={{ color: item.iconColor }} />
      </div>
      <div className="min-w-0 flex-1">
        <p className="font-semibold text-sm" style={{ color: "var(--text)" }}>
          {item.label}
        </p>
        <p
          className="text-xs mt-0.5 leading-snug"
          style={{ color: "var(--text-muted)" }}
        >
          {item.description}
        </p>
      </div>
      <ArrowRight
        size={15}
        className="shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
        style={{ color: "var(--text-muted)" }}
      />
    </Link>
  );
}

export default function AtendimentoHub() {
  const [highlight, ...rest] = items;

  return (
    <div style={{ backgroundColor: "var(--bg)" }}>
      <div className="mx-auto max-w-2xl px-4 pb-12 pt-6">
        <PageHeader
          title="Atendimento"
          subtitle="Central de atendimento ao cliente - pedidos e cadastros"
        />

        <div className="space-y-3">
          <HighlightCard item={highlight} />
          {rest.map((item) => (
            <ActionCard key={item.to} item={item} />
          ))}
        </div>
      </div>
    </div>
  );
}
