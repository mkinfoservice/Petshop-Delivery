/**
 * Fluxo: Vincular cliente ao pedido
 * Módulo: Pedidos
 * Vídeo: 04-vincular-cliente
 *
 * Roteiro completo em: automation/roteiros/pedidos/04-vincular-cliente.md
 *
 * Setup: cria cliente via API no beforeAll; demonstra a busca pelo telefone
 * e o vínculo ao novo pedido; remove o cliente via API no afterAll.
 */

import { test, request } from "@playwright/test";
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

// ── Dados ─────────────────────────────────────────────────────────────────────
function gerarTelefone(): string {
  const d = new Date();
  const mm  = String(d.getMonth() + 1).padStart(2, "0");
  const dd  = String(d.getDate()).padStart(2, "0");
  const hh  = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  // "417" (3) + mm(2) + dd(2) + hh(2) + min(2) = 11 dígitos
  return `417${mm}${dd}${hh}${min}`;
}

const CLIENTE = { nome: "Gabriela Rocha", telefone: gerarTelefone() };
let clienteId: string | null = null;

const LEGENDAS = {
  busca:
    "Digite o telefone ou CPF do cliente no campo de busca.\nO sistema localiza o cadastro em tempo real.",
  encontrado:
    "Cliente encontrado!\nO sistema exibe o nome, telefone e endereço do cadastro.",
  confirmando:
    "Clique em Confirmar e montar pedido para vincular\neste cliente ao novo atendimento.",
  vinculado:
    "Cliente vinculado! O carrinho de produtos é aberto\ncom os dados do cliente já preenchidos.",
} as const;

// ── Setup / Teardown ──────────────────────────────────────────────────────────

test.beforeAll(async () => {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) { await api.dispose(); return; }
  const { token } = await loginRes.json();

  const res = await api.post("/admin/customers", {
    headers: { Authorization: `Bearer ${token}` },
    data: { name: CLIENTE.nome, phone: CLIENTE.telefone },
  });
  if (res.ok()) {
    const body = await res.json();
    clienteId = body.id ?? body.customerId ?? null;
    console.log(`[setup] Cliente para vincular criado: ${clienteId}`);
  }
  await api.dispose();
});

test.afterAll(async () => {
  if (!clienteId) return;
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) { await api.dispose(); return; }
  const { token } = await loginRes.json();
  await api.delete(`/admin/customers/${clienteId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  await api.dispose();
});

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Pedidos", () => {
  test("04 — Vincular cliente ao pedido", async ({ page }) => {

    await configurarTema(page);

    await navegarPara(page, "/app/atendimento/pedido");
    await injetarUITutorial(page);
    await page.waitForSelector("text=Novo Atendimento", { state: "visible", timeout: 15_000 });

    // ── Cena 1: Digitar telefone ─────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.busca);
    await pausaVisual(800);

    const campoTelefone = page.getByPlaceholder(/Telefone ou CPF/i);
    for (const char of CLIENTE.telefone) {
      await campoTelefone.type(char, { delay: 60 });
    }
    await pausaVisual(500);
    await ocultarLegenda(page);

    // Dispara a busca
    await page.getByRole("button", { name: /Buscar/i }).click();
    await pausaVisual(1_500);

    // ── Cena 2: Cliente encontrado ───────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.encontrado);
    await page.waitForSelector("text=Cliente encontrado", { timeout: 10_000 });
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 3: Confirmar ────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.confirmando);
    await pausaVisual(800);
    await page.getByRole("button", { name: /Confirmar e montar pedido/i }).click();
    await aguardarCarregamento(page);

    // ── Cena 4: Carrinho aberto ──────────────────────────────────────────
    // Espera o campo de busca do carrinho aparecer
    await page.waitForSelector('[placeholder*="Buscar produto"]', { timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.vinculado);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
