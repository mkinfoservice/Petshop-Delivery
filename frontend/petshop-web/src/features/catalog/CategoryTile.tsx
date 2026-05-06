import type { JSX } from "react";

type Category = {
  id: string;
  name: string;
  slug: string;
};

// ── Pet Category SVG Icons ─────────────────────────────────────────────────

function isDog(n: string)      { return n.includes("cão") || n.includes("cao") || n.includes("cães") || n.includes("caes") || n.includes("cachorro") || n.includes("dog"); }
function isCat(n: string)      { return n.includes("gato") || n.includes("felino") || n.includes("cat"); }
function isBird(n: string)     { return n.includes("ave") || n.includes("pássaro") || n.includes("passaro") || n.includes("bird") || n.includes("periquito") || n.includes("calopsita"); }
function isFish(n: string)     { return n.includes("peixe") || n.includes("aquário") || n.includes("aquario") || n.includes("fish") || n.includes("tartaruga"); }
function isRodent(n: string)   { return n.includes("roedor") || n.includes("hamster") || n.includes("coelho") || n.includes("cobaia") || n.includes("rato"); }
function isPharmacy(n: string) { return n.includes("farmácia") || n.includes("farmacia") || n.includes("remédio") || n.includes("remedio") || n.includes("medicament") || n.includes("veterinár") || n.includes("saúde") || n.includes("saude"); }
function isFood(n: string)     { return n.includes("ração") || n.includes("racao") || n.includes("aliment") || n.includes("petisco") || n.includes("comida") || n.includes("snack") || n.includes("trat"); }
function isToy(n: string)      { return n.includes("brinquedo") || n.includes("toy") || n.includes("osso") || n.includes("bone"); }
function isHygiene(n: string)  { return n.includes("higiene") || n.includes("shampoo") || n.includes("banho") || n.includes("limpeza") || n.includes("grooming") || n.includes("tosa"); }
function isAccessory(n: string){ return n.includes("acessório") || n.includes("acessorio") || n.includes("coleira") || n.includes("roupa") || n.includes("cama") || n.includes("colar") || n.includes("guia"); }
function isOtherPet(n: string) { return n.includes("outro") || n.includes("réptil") || n.includes("reptil") || n.includes("exótico") || n.includes("exotico"); }

export function petCategoryIcon(name: string, size = 16): JSX.Element {
  const n = name.toLocaleLowerCase();
  const p = { width: size, height: size, viewBox: "0 0 24 24", fill: "currentColor", "aria-hidden": true };

  if (isDog(n)) return (
    <svg {...p}>
      {/* dog face */}
      <ellipse cx="6" cy="5" rx="2.2" ry="3.2" />
      <ellipse cx="18" cy="5" rx="2.2" ry="3.2" />
      <path d="M12 6C8.69 6 6 8.69 6 12v2c0 2.21 2.69 4 6 4s6-1.79 6-4v-2c0-3.31-2.69-6-6-6z" />
      <circle cx="10" cy="12.5" r="0.9" fill="white" />
      <circle cx="14" cy="12.5" r="0.9" fill="white" />
      <ellipse cx="12" cy="14.5" rx="1.2" ry="0.7" fill="white" />
    </svg>
  );

  if (isCat(n)) return (
    <svg {...p}>
      {/* cat face */}
      <path d="M4 3 L4 9 L7 7 Z" />
      <path d="M20 3 L20 9 L17 7 Z" />
      <circle cx="12" cy="13" r="7" />
      <circle cx="9.5" cy="12" r="0.9" fill="white" />
      <circle cx="14.5" cy="12" r="0.9" fill="white" />
      <path d="M10 15 Q12 16.5 14 15" stroke="white" strokeWidth="0.9" fill="none" strokeLinecap="round" />
      <line x1="6" y1="12" x2="9" y2="13" stroke="white" strokeWidth="0.7" />
      <line x1="6" y1="14" x2="9" y2="14" stroke="white" strokeWidth="0.7" />
      <line x1="18" y1="12" x2="15" y2="13" stroke="white" strokeWidth="0.7" />
      <line x1="18" y1="14" x2="15" y2="14" stroke="white" strokeWidth="0.7" />
    </svg>
  );

  if (isBird(n)) return (
    <svg {...p}>
      {/* bird silhouette */}
      <ellipse cx="12" cy="13" rx="6" ry="5" />
      <ellipse cx="12" cy="8" rx="3.5" ry="3.5" />
      <path d="M15.5 8 Q19 6 20 8 Q18 9.5 15.5 9.5Z" />
      <circle cx="13.5" cy="7" r="1" fill="white" />
      <path d="M8 10 Q4 8 2 10 Q4 12 8 11Z" />
      <path d="M12 17.5 L11 21 L13 21Z" />
    </svg>
  );

  if (isFish(n)) return (
    <svg {...p}>
      {/* fish */}
      <path d="M3 12 L7 8 L7 16 Z" />
      <ellipse cx="14" cy="12" rx="7" ry="5" />
      <circle cx="18" cy="10.5" r="1" fill="white" />
      <line x1="9" y1="10" x2="9" y2="14" stroke="white" strokeWidth="0.8" />
      <line x1="11.5" y1="9" x2="11.5" y2="15" stroke="white" strokeWidth="0.8" />
    </svg>
  );

  if (isRodent(n)) return (
    <svg {...p}>
      {/* bunny/rodent */}
      <ellipse cx="9" cy="5" rx="2" ry="4.5" />
      <ellipse cx="15" cy="5" rx="2" ry="4.5" />
      <ellipse cx="12" cy="14.5" rx="5.5" ry="5.5" />
      <circle cx="10" cy="13.5" r="0.9" fill="white" />
      <circle cx="14" cy="13.5" r="0.9" fill="white" />
      <ellipse cx="12" cy="15.5" rx="1" ry="0.6" fill="white" />
    </svg>
  );

  if (isPharmacy(n)) return (
    <svg {...p}>
      {/* medical cross */}
      <rect x="9" y="3" width="6" height="18" rx="2" />
      <rect x="3" y="9" width="18" height="6" rx="2" />
    </svg>
  );

  if (isFood(n)) return (
    <svg {...p}>
      {/* food bowl with paw */}
      <path d="M4 13 Q4 19 12 19 Q20 19 20 13 Z" />
      <path d="M3 12 Q3 10 12 10 Q21 10 21 12 L21 13 L3 13 Z" />
      <circle cx="10" cy="7" r="1.5" />
      <circle cx="14" cy="6" r="1.5" />
      <circle cx="8" cy="4.5" r="1.2" />
      <path d="M12 8C10 8 8.5 9.2 8.5 10.5h7C15.5 9.2 14 8 12 8z" />
    </svg>
  );

  if (isToy(n)) return (
    <svg {...p}>
      {/* bone */}
      <circle cx="5.5" cy="5.5" r="2.5" />
      <circle cx="18.5" cy="5.5" r="2.5" />
      <circle cx="5.5" cy="18.5" r="2.5" />
      <circle cx="18.5" cy="18.5" r="2.5" />
      <rect x="4" y="10" width="16" height="4" rx="2" />
      <rect x="10" y="4" width="4" height="16" rx="2" />
    </svg>
  );

  if (isHygiene(n)) return (
    <svg {...p}>
      {/* soap/shampoo bubble */}
      <path d="M12 3C8 3 5 6.5 5 10.5c0 5.5 3.5 9.5 7 10 3.5-.5 7-4.5 7-10C19 6.5 16 3 12 3z" />
      <circle cx="9" cy="9" r="1.2" fill="white" opacity="0.7" />
      <circle cx="13.5" cy="7.5" r="0.8" fill="white" opacity="0.7" />
      <circle cx="15" cy="12" r="1" fill="white" opacity="0.5" />
    </svg>
  );

  if (isAccessory(n)) return (
    <svg {...p}>
      {/* collar with tag */}
      <path d="M5 10 Q5 5 12 5 Q19 5 19 10 Q19 14 12 15 Q5 14 5 10Z" fill="none" stroke="currentColor" strokeWidth="2.5" />
      <rect x="10" y="14" width="4" height="5" rx="1.5" />
      <circle cx="12" cy="10" r="1" />
    </svg>
  );

  if (isOtherPet(n)) return (
    <svg {...p}>
      {/* generic pet star/others */}
      <path d="M12 2l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 17l-6.2 4.3 2.4-7.4L2 9.4h7.6z" />
    </svg>
  );

  // Default: paw print
  return (
    <svg {...p}>
      <circle cx="7.5" cy="5.2" r="2.2" />
      <circle cx="12" cy="4" r="2.2" />
      <circle cx="16.5" cy="5.2" r="2.2" />
      <circle cx="5" cy="9.5" r="2" />
      <path d="M12 9c-3.3 0-6 2.2-6 5.5C6 17.5 8.7 19 12 19s6-1.5 6-4.5C18 11.2 15.3 9 12 9z" />
    </svg>
  );
}

// ── CategoryTile component ──────────────────────────────────────────────────

export function CategoryTile({
  c,
  active,
  onClick,
}: {
  c: Category;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-2 px-4 py-2 rounded-full text-sm font-bold whitespace-nowrap transition-all shrink-0 select-none active:scale-95"
      style={active
        ? {
            background: "var(--brand)",
            color: "#fff",
            boxShadow: "0 4px 14px color-mix(in srgb, var(--brand) 38%, transparent)",
          }
        : {
            background: "var(--surface)",
            color: "var(--text-muted)",
            border: "1.5px solid var(--border)",
          }
      }
    >
      <span className="shrink-0 leading-none" style={{ opacity: active ? 1 : 0.75 }}>
        {petCategoryIcon(c.name, 15)}
      </span>
      <span>{c.name}</span>
    </button>
  );
}
