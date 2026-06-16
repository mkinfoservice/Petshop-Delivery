/**
 * Fluxo: Finalizar pedido
 * Módulo: Pedidos
 * Vídeo: 07-finalizar-pedido
 *
 * Roteiro completo em: automation/roteiros/pedidos/07-finalizar-pedido.md
 *
 * Setup: tenta criar pedido via API; fallback busca pedido RECEBIDO existente.
 */

import { test, expect, request } from "@playwright/test";
import {
  navegarPara,
  aguardarCarregamento,
  pausaVisual,
} from "../../helpers/navigation";
import {
  configurarTema,
  injetarUITutorial,
  mostrarLegenda,
  ocultarLegenda,
} from "../../helpers/tutorial";
import { CREDENCIAIS } from "../../auth/credenciais";

let orderNumber: string | null = null;
let orderId:     string | null = null;
let orderCreatedByTest = false;

const LEGENDAS = {
  fluxo:
    "O fluxo de atendimento mostra cada etapa até a entrega.\nClique nos botões para avançar o status.",
  prontinho:
    "Clique em Pronto para servir quando o pedido estiver preparado.",
  entregando:
    "Clique em Entregue para registrar que o atendimento foi concluído.",
  finalizado:
    "Pedido finalizado como Entregue!\nO atendimento fica registrado no histórico.",
} as const;

async function autenticarAPI() {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) return { api, token: null as null };
  const { token } = await loginRes.json();
  return { api, token: token as string };
}

test.beforeAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  const headers = { Authorization: `Bearer ${token}` };

  // 1ª tentativa: criar pedido do zero
  const prodRes = await api.get("/admin/products?active=true&pageSize=1", { headers });
  let productId: string | null = null;
  if (prodRes.ok()) {
    const body = await prodRes.json();
    productId = body.items?.[0]?.id ?? null;
  }

  if (productId) {
    const createRes = await api.post("/admin/orders/phone", {
      headers,
      data: {
        customerName:  "Juliana Alves",
        customerPhone: "11955500007",
        items:         [{ productId, qty: 1 }],
        paymentMethod: "PIX",
      },
    });

    if (createRes.ok()) {
      const body = await createRes.json();
      orderId     = body.id ?? null;
      orderNumber = body.orderNumber ?? null;
      orderCreatedByTest = true;

      // Avança para EM_PREPARO
      if (orderId) {
        await api.patch(`/orders/${orderId}/status`, {
          headers,
          data: { status: "EM_PREPARO" },
        });
      }
      console.log(`[setup] Pedido de finalização criado: ${orderNumber}`);
    }
  }

  // Fallback: busca pedido RECEBIDO existente na API
  if (!orderNumber) {
    const listRes = await api.get("/admin/orders?status=RECEBIDO&pageSize=5", { headers });
    if (listRes.ok()) {
      const body = await listRes.json();
      const items: Array<{ id: string; publicId?: string; orderNumber?: string }> = body.items ?? body ?? [];
      const first = items[0];
      if (first) {
        orderId     = first.id ?? null;
        orderNumber = first.publicId ?? first.orderNumber ?? null;
        console.log(`[setup] Usando pedido RECEBIDO existente: ${orderNumber}`);
      }
    }
  }

  if (!orderNumber) {
    console.warn("[setup] Nenhum pedido disponível para o tutorial de finalização.");
  }

  await api.dispose();
});

test.describe("Módulo: Pedidos", () => {
  test("07 — Finalizar pedido", async ({ page }) => {

    test.skip(!orderNumber, "Nenhum pedido disponível para o tutorial");

    await configurarTema(page);

    await navegarPara(page, `/app/pedidos/${orderNumber}`);
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });
    await aguardarCarregamento(page);

    // ── Cena 1: Fluxo de status ──────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.fluxo);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Pronto para servir ───────────────────────────────────────
    // Avança até aparecer o botão "Pronto para servir" (pode estar em RECEBIDO ou EM_PREPARO)
    const btnPronto = page.getByRole("button", { name: /Pronto para servir/i }).first();
    if (await btnPronto.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await mostrarLegenda(page, LEGENDAS.prontinho);
      await pausaVisual(800);
      await btnPronto.click();
      await aguardarCarregamento(page);
      await pausaVisual(1_200);
      await ocultarLegenda(page);
    }

    // ── Cena 3: Entregue ─────────────────────────────────────────────────
    const btnEntregue = page.getByRole("button", { name: /^Entregue$/i }).first();
    await btnEntregue.waitFor({ state: "visible", timeout: 10_000 });

    await mostrarLegenda(page, LEGENDAS.entregando);
    await pausaVisual(800);
    await btnEntregue.click();
    await aguardarCarregamento(page);
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 4: Finalizado ───────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.finalizado);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
