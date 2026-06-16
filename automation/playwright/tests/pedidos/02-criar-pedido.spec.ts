/**
 * Fluxo: Criar pedido
 * Módulo: Pedidos
 * Vídeo: 02-criar-pedido
 *
 * Roteiro completo em: automation/roteiros/pedidos/02-criar-pedido.md
 *
 * Setup: cria cliente via API no beforeAll; percorre o wizard completo
 * (busca → carrinho → pagamento → resumo → confirmação) via UI;
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

// ── Dados ─────────────────────────────────────────────────────────────────────
function gerarTelefone(): string {
  const d = new Date();
  const mm  = String(d.getMonth() + 1).padStart(2, "0");
  const dd  = String(d.getDate()).padStart(2, "0");
  const hh  = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  // "319" (3) + mm(2) + dd(2) + hh(2) + min(2) = 11 dígitos
  return `319${mm}${dd}${hh}${min}`;
}

const CLIENTE = { nome: "Fernando Gomes", telefone: gerarTelefone() };
let clienteId: string | null = null;
let orderId:   string | null = null;

const LEGENDAS = {
  busca:
    "Digite o telefone ou CPF do cliente para localizá-lo no sistema.",
  encontrado:
    "Cliente encontrado! Confirme para montar o pedido.",
  carrinho:
    "Selecione os produtos do catálogo.\nClique para adicionar ao carrinho.",
  pagamento:
    "Escolha a forma de pagamento e revise o total do pedido.",
  resumo:
    "Confirme os dados do pedido antes de finalizar.",
  confirmado:
    "Pedido confirmado!\nO número fica registrado para acompanhamento.",
} as const;

// ── Helpers de API ────────────────────────────────────────────────────────────

async function autenticarAPI() {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) return { api, token: null };
  const { token } = await loginRes.json();
  return { api, token: token as string };
}

// ── Setup / Teardown ──────────────────────────────────────────────────────────

test.beforeAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }

  const res = await api.post("/admin/customers", {
    headers: { Authorization: `Bearer ${token}` },
    data: { name: CLIENTE.nome, phone: CLIENTE.telefone },
  });
  if (res.ok()) {
    const body = await res.json();
    clienteId = body.id ?? body.customerId ?? null;
    console.log(`[setup] Cliente para criar pedido: ${clienteId}`);
  }
  await api.dispose();
});

test.afterAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  const headers = { Authorization: `Bearer ${token}` };

  if (orderId) {
    await api.patch(`/orders/${orderId}/status`, {
      headers,
      data: { status: "CANCELADO" },
    });
    console.log(`[teardown] Pedido de demo cancelado.`);
  }
  if (clienteId) {
    await api.delete(`/admin/customers/${clienteId}`, { headers });
    console.log(`[teardown] Cliente de demo removido.`);
  }
  await api.dispose();
});

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Pedidos", () => {
  test("02 — Criar pedido", async ({ page }) => {

    test.setTimeout(420_000); // 7 min — margem para slowMo 600ms + pauses + rede lenta
    test.skip(!clienteId, "Cliente de teste não foi criado no beforeAll");

    await configurarTema(page);

    await navegarPara(page, "/app/atendimento/pedido");
    await injetarUITutorial(page);
    await page.waitForSelector("text=Novo Atendimento", { state: "visible", timeout: 15_000 });

    // ── Cena 1: Busca do cliente ─────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.busca);
    await pausaVisual(800);

    const campoTelefone = page.getByPlaceholder(/Telefone ou CPF/i);
    for (const char of CLIENTE.telefone) {
      await campoTelefone.type(char, { delay: 60 });
    }
    await page.getByRole("button", { name: /Buscar/i }).click();
    await page.waitForSelector("text=Cliente encontrado", { timeout: 12_000 });
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 2: Confirmar cliente ────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.encontrado);
    await pausaVisual(1_000);
    await page.getByRole("button", { name: /Confirmar e montar pedido/i }).click();
    await aguardarCarregamento(page);

    // Aguarda campo de busca do carrinho
    await page.waitForSelector('[placeholder*="Buscar produto"]', { timeout: 15_000 });
    await ocultarLegenda(page);

    // ── Cena 3: Adicionar produto ao carrinho ────────────────────────────
    await mostrarLegenda(page, LEGENDAS.carrinho);
    await pausaVisual(1_000);

    // Aguarda o grid de produtos (os botões de categoria têm a mesma classe text-left rounded-2xl)
    await page.waitForSelector('div.grid.gap-3 button.text-left.rounded-2xl', { timeout: 15_000 });
    const primeiroProduto = page.locator('div.grid.gap-3 button.text-left.rounded-2xl').first();
    await primeiroProduto.click();

    // Se o produto tem adicionais, abre modal com botão "Adicionar".
    // Aguarda até 6s pelo botão — tempo suficiente para fetch de detalhes + slowMo.
    // Se o botão não aparecer (produto sem adicionais), foi adicionado direto ao carrinho.
    const botaoAdicionar = page.getByRole("button", { name: /^Adicionar$/ });
    const modalAberto = await botaoAdicionar
      .waitFor({ state: "visible", timeout: 6_000 })
      .then(() => true)
      .catch(() => false);

    if (modalAberto) {
      // Aguarda o spinner SVG (Loader2.animate-spin) desaparecer — indica que
      // configuringProduct foi carregado e confirmProductConfig() não retornará early.
      await page.waitForFunction(
        () => !document.querySelector("svg.animate-spin"),
        { timeout: 15_000 }
      );
      await pausaVisual(1_200);
      await botaoAdicionar.click();
      await pausaVisual(1_000);
    } else {
      // Produto sem adicionais → já foi adicionado direto ao carrinho pelo click
      await pausaVisual(1_200);
    }

    // Aguarda o botão "Ir para pagamento" ficar habilitado (carrinho com ≥1 item)
    await page.waitForFunction(
      () => {
        const btns = [...document.querySelectorAll("button")];
        const btn = btns.find((b) => /Ir para pagamento/i.test(b.textContent ?? ""));
        return btn && !(btn as HTMLButtonElement).disabled;
      },
      { timeout: 20_000 }
    );

    await pausaVisual(1_000);
    await ocultarLegenda(page);

    // Avança para pagamento (botão fixo no rodapé)
    await page.getByRole("button", { name: /Ir para pagamento/i }).click();
    await aguardarCarregamento(page);

    // ── Cena 4: Pagamento ────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.pagamento);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // PIX é o padrão — avança para o resumo
    await page.getByRole("button", { name: /Ver resumo/i }).click();
    await pausaVisual(600);

    // ── Cena 5: Resumo ───────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.resumo);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // Confirma o pedido
    await page.getByRole("button", { name: /Confirmar pedido/i }).click();

    // Aguarda tela de sucesso
    await page.waitForSelector("text=Pedido confirmado!", { timeout: 20_000 });
    await aguardarCarregamento(page);

    // Captura o ID do pedido a partir da URL ou do link "Ver pedido"
    const verPedidoLink = page.getByRole("button", { name: /Ver pedido/i });
    if (await verPedidoLink.isVisible({ timeout: 2_000 }).catch(() => false)) {
      const href = await verPedidoLink.evaluate((el) => (el as HTMLAnchorElement).href ?? "");
      const segments = href.split("/");
      orderId = segments[segments.length - 1] || null;
    }

    // ── Cena 6: Pedido confirmado ────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.confirmado);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    await expect(page.locator("h2")).toContainText(/Pedido confirmado!/i);
  });
});
