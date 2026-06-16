/**
 * helpers/tutorial.ts
 *
 * Recursos visuais para gravação de tutoriais no vendApps:
 *
 *  1. LIGHT MODE   — força modo claro via localStorage antes do React montar
 *  2. CURSOR       — cursor personalizado visível sobre a página
 *  3. LEGENDAS     — legendas do roteiro fixadas no rodapé
 *
 * Nota: ripple, seta ▼ e contorno do elemento clicado foram removidos —
 * ficavam fora de sincronização com o slowMo e prejudicavam a qualidade visual.
 *
 * Como usar num teste:
 *
 *   test('fluxo', async ({ page }) => {
 *     await configurarTema(page);               // ANTES do primeiro goto()
 *     await page.goto('/app');
 *     await injetarUITutorial(page);            // APÓS a página carregar
 *
 *     await mostrarLegenda(page, 'Texto aqui');
 *     // ... ações do teste ...
 *     await ocultarLegenda(page);
 *   });
 */

import { Page } from "@playwright/test";

// ─────────────────────────────────────────────────────────────────────────────
// 1. LIGHT MODE
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Força o modo claro (light mode) no vendApps.
 * IMPORTANTE: chamar ANTES do primeiro page.goto().
 */
export async function configurarTema(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem("theme", "light");
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// 2-3. CURSOR + LEGENDAS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Injeta todos os elementos visuais do tutorial na página atual.
 * Como é uma SPA, basta chamar UMA VEZ — os elementos persistem nas rotas.
 * IMPORTANTE: chamar APÓS aguardarCarregamento().
 */
export async function injetarUITutorial(page: Page): Promise<void> {
  await page.addStyleTag({
    content: `
      /* ── Cursor nativo oculto ── */
      *, *::before, *::after { cursor: none !important; }

      /* ── Cursor personalizado ── */
      #pw-cursor {
        position: fixed;
        width: 24px;
        height: 24px;
        border-radius: 50%;
        background: rgba(120, 88, 250, 0.92);
        border: 3px solid rgba(255, 255, 255, 0.96);
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.45);
        pointer-events: none;
        z-index: 2147483647;
        transform: translate(-50%, -50%);
        will-change: left, top;
      }
      /* Estado de clique: menor e mais escuro */
      #pw-cursor.clicking {
        background: rgba(80, 50, 200, 0.95);
        transform: translate(-50%, -50%) scale(0.72);
      }

      /* ── Legenda / Subtitle ── */
      #pw-caption {
        position: fixed;
        bottom: 28px;
        left: 50%;
        transform: translateX(-50%);
        max-width: 72%;
        padding: 10px 22px;
        background: rgba(8, 8, 8, 0.82);
        color: #ffffff;
        font-size: 15px;
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        font-weight: 500;
        line-height: 1.55;
        text-align: center;
        border-radius: 8px;
        box-shadow: 0 2px 20px rgba(0, 0, 0, 0.5);
        pointer-events: none;
        z-index: 2147483646;
        display: none;
        letter-spacing: 0.01em;
      }
    `,
  });

  await page.evaluate(() => {
    // ── Cursor ──────────────────────────────────────────────────────────────
    document.getElementById("pw-cursor")?.remove();
    const cursor = document.createElement("div");
    cursor.id = "pw-cursor";
    document.body.appendChild(cursor);

    document.addEventListener("mousemove", (e) => {
      cursor.style.left = `${e.clientX}px`;
      cursor.style.top  = `${e.clientY}px`;
    });

    let releaseTimer: ReturnType<typeof window.setTimeout> | null = null;

    document.addEventListener("mousedown", () => {
      if (releaseTimer !== null) {
        window.clearTimeout(releaseTimer);
        releaseTimer = null;
      }
      cursor.classList.add("clicking");
    });

    document.addEventListener("mouseup", () => {
      releaseTimer = window.setTimeout(() => {
        cursor.classList.remove("clicking");
        releaseTimer = null;
      }, 200);
    });

    // ── Legenda ─────────────────────────────────────────────────────────────
    document.getElementById("pw-caption")?.remove();
    const caption = document.createElement("div");
    caption.id = "pw-caption";
    document.body.appendChild(caption);
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// Overlay WhatsApp
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Injeta uma notificação estilo WhatsApp no canto superior direito da tela.
 * Usa slide-in / auto-dismiss após ~4 s. Ideal para mostrar o que o cliente recebe.
 *
 * @param tipo     "entregue" | "falha" — define o texto da mensagem
 * @param cliente  Nome do cliente (ex: "João Silva")
 * @param pedido   Número do pedido (ex: "PS-20260416-001")
 */
export async function mostrarOverlayWhatsApp(
  page: Page,
  tipo: "entregue" | "falha",
  cliente: string,
  pedido: string,
): Promise<void> {
  const msg =
    tipo === "entregue"
      ? `Olá, ${cliente}! ✅ Seu pedido *${pedido}* foi entregue com sucesso! Obrigado pela preferência. 🙏`
      : `Olá, ${cliente}. Infelizmente não conseguimos entregar seu pedido *${pedido}*. Entre em contato para reagendar. 📦`;

  await page.evaluate(({ mensagem }: { mensagem: string }) => {
    document.getElementById("pw-wapp-overlay")?.remove();

    const overlay = document.createElement("div");
    overlay.id = "pw-wapp-overlay";
    overlay.style.cssText = [
      "position: fixed",
      "top: 20px",
      "right: 20px",
      "width: 290px",
      "background: #fff",
      "border-radius: 12px",
      "box-shadow: 0 6px 28px rgba(0,0,0,0.28)",
      "z-index: 2147483646",
      "overflow: hidden",
      "font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
      "transform: translateX(120%)",
      "transition: transform 0.45s cubic-bezier(0.34,1.56,0.64,1)",
    ].join("; ");

    overlay.innerHTML = `
      <div style="background:#25D366;padding:10px 14px;display:flex;align-items:center;gap:10px;">
        <div style="width:34px;height:34px;background:rgba(255,255,255,0.2);border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:18px;flex-shrink:0;">📦</div>
        <div style="flex:1;min-width:0;">
          <div style="color:#fff;font-size:12px;font-weight:600;">vendApps Delivery</div>
          <div style="color:rgba(255,255,255,0.75);font-size:11px;">via WhatsApp · agora</div>
        </div>
      </div>
      <div style="padding:12px 14px;background:#ECE5DD;">
        <div style="background:#fff;border-radius:0 8px 8px 8px;padding:9px 12px;font-size:13px;color:#111;line-height:1.55;box-shadow:0 1px 2px rgba(0,0,0,0.1);white-space:pre-line;">${mensagem}</div>
      </div>
    `;

    document.body.appendChild(overlay);

    // slide-in
    window.setTimeout(() => { overlay.style.transform = "translateX(0)"; }, 50);

    // slide-out após 4 s
    window.setTimeout(() => {
      overlay.style.transition = "transform 0.4s ease-in, opacity 0.4s ease-in";
      overlay.style.transform  = "translateX(120%)";
      overlay.style.opacity    = "0";
      window.setTimeout(() => overlay.remove(), 450);
    }, 4_000);
  }, { mensagem: msg });

  await page.waitForTimeout(4_500); // aguarda o overlay sumir antes de prosseguir
}

// ─────────────────────────────────────────────────────────────────────────────
// API de legendas
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Exibe uma legenda na parte inferior da tela.
 * Permanece visível até que `ocultarLegenda()` seja chamada.
 */
export async function mostrarLegenda(page: Page, texto: string): Promise<void> {
  await page.evaluate((t) => {
    const el = document.getElementById("pw-caption");
    if (!el) return;
    el.innerHTML = t.replace(/\n/g, "<br>");
    el.style.display = "block";
  }, texto);
}

/**
 * Oculta a legenda atual.
 */
export async function ocultarLegenda(page: Page): Promise<void> {
  await page.evaluate(() => {
    const el = document.getElementById("pw-caption");
    if (el) el.style.display = "none";
  });
}
