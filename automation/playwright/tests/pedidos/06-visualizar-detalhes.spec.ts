/**
 * Fluxo: Visualizar detalhes do pedido
 * Módulo: Pedidos
 * Vídeo: 06-visualizar-detalhes
 *
 * Roteiro completo em: automation/roteiros/pedidos/06-visualizar-detalhes.md
 *
 * Sem setup/teardown — usa o primeiro pedido existente na lista.
 */

import { test } from "@playwright/test";
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

const LEGENDAS = {
  lista:
    "Na lista de pedidos, clique em qualquer linha para abrir os detalhes completos.",
  cabecalho:
    "O cabeçalho exibe o número do pedido, o status atual e os botões de ação.",
  status:
    "O painel de status mostra o fluxo completo do atendimento.\nO botão iluminado indica o status atual.",
  itens:
    "Os itens mostram produto, quantidade e valor unitário.\nO total é calculado automaticamente.",
  pagamento:
    "Forma de pagamento e dados do cliente ficam registrados\npara consulta e comprovante.",
} as const;

test.describe("Módulo: Pedidos", () => {
  test("06 — Visualizar detalhes do pedido", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de pedidos ──────────────────────────────────────────
    await navegarPara(page, "/app/pedidos");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // A lista usa <tr onClick> — clica na primeira linha da tabela de pedidos
    const primeiroPedido = page.locator("tbody tr").first();
    await primeiroPedido.waitFor({ state: "visible", timeout: 15_000 });
    await primeiroPedido.click();
    await aguardarCarregamento(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    // ── Cena 2: Cabeçalho ────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.cabecalho);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 3: Painel de status ─────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.status);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 4: Itens do pedido ──────────────────────────────────────────
    await page.mouse.wheel(0, 300);
    await pausaVisual(600);
    await mostrarLegenda(page, LEGENDAS.itens);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 5: Pagamento e cliente ──────────────────────────────────────
    await page.mouse.wheel(0, 300);
    await pausaVisual(600);
    await mostrarLegenda(page, LEGENDAS.pagamento);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
