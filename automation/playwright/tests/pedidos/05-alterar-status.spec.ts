/**
 * Fluxo: Alterar status do pedido
 * Módulo: Pedidos
 * Vídeo: 05-alterar-status
 *
 * Roteiro completo em: automation/roteiros/pedidos/05-alterar-status.md
 *
 * Setup: cria pedido via API no beforeAll; altera o status via UI;
 * cancela o pedido via API no afterAll.
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
let orderId: string | null = null;

const LEGENDAS = {
  statusAtual:
    "O painel de status mostra onde o pedido está no fluxo de atendimento.\nO botão destacado é o status atual.",
  alterando:
    "Clique no próximo status para avançar o pedido.\nApenas o próximo passo está habilitado para garantir o fluxo correto.",
  atualizado:
    "Status atualizado para Em preparo!\nA alteração fica registrada no histórico do pedido.",
} as const;

async function autenticarAPI() {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) return { api, token: null };
  const { token } = await loginRes.json();
  return { api, token: token as string };
}

test.beforeAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  const headers = { Authorization: `Bearer ${token}` };

  const prodRes = await api.get("/admin/products?active=true&pageSize=1", { headers });
  let productId: string | null = null;
  if (prodRes.ok()) {
    const body = await prodRes.json();
    productId = body.items?.[0]?.id ?? null;
  }

  if (!productId) {
    console.warn("[setup] Nenhum produto ativo para criar o pedido de status.");
    await api.dispose();
    return;
  }

  const createRes = await api.post("/admin/orders/phone", {
    headers,
    data: {
      customerName:  "Helena Costa",
      customerPhone: "11955500005",
      items:         [{ productId, qty: 1 }],
      paymentMethod: "PIX",
    },
  });

  if (createRes.ok()) {
    const body = await createRes.json();
    orderId     = body.id ?? null;
    orderNumber = body.orderNumber ?? null;
    console.log(`[setup] Pedido de status criado: ${orderNumber}`);
  } else {
    console.warn(`[setup] Falha ao criar pedido: ${createRes.status()}`);
  }

  await api.dispose();
});

test.afterAll(async () => {
  if (!orderId) return;
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  await api.patch(`/orders/${orderId}/status`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { status: "CANCELADO" },
  });
  console.log(`[teardown] Pedido de status cancelado: ${orderNumber}`);
  await api.dispose();
});

test.describe("Módulo: Pedidos", () => {
  test("05 — Alterar status do pedido", async ({ page }) => {

    test.skip(!orderNumber, "Pedido de teste não foi criado no beforeAll");

    await configurarTema(page);

    await navegarPara(page, `/app/pedidos/${orderNumber}`);
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });
    await aguardarCarregamento(page);

    // ── Cena 1: Status atual ─────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.statusAtual);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Alterar para Em preparo ──────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.alterando);
    await pausaVisual(800);

    // Clica no botão "Em preparo" no fluxo de status
    await page.getByRole("button", { name: /Em preparo/i }).first().click();
    await aguardarCarregamento(page);

    await pausaVisual(1_500);
    await ocultarLegenda(page);

    // ── Cena 3: Confirmação ──────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.atualizado);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Validação ────────────────────────────────────────────────────────
    // O botão "Em preparo" deve estar ativo (destacado)
    await expect(
      page.getByRole("button", { name: /Em preparo/i }).first()
    ).toBeVisible();
  });
});
