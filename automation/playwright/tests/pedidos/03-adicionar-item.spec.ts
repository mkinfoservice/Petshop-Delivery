/**
 * Fluxo: Adicionar item ao pedido
 * Módulo: Pedidos
 * Vídeo: 03-adicionar-item
 *
 * Roteiro completo em: automation/roteiros/pedidos/03-adicionar-item.md
 *
 * Demonstra o passo de montagem do carrinho no PhoneOrderBuilder.
 * Usa modo "Sem cadastro" para chegar ao carrinho sem beforeAll.
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
  busca:
    "Digite o nome do produto no campo de busca\npara filtrar o catálogo em tempo real.",
  adicionando:
    "Clique no produto para adicioná-lo ao carrinho.\nSe houver adicionais, uma tela de configuração é exibida.",
  carrinho:
    "O carrinho mostra os itens selecionados com quantidade e valor.\nAjuste a quantidade com os botões + e −.",
  pronto:
    "Com o carrinho montado, clique em Ir para pagamento\npara prosseguir com o atendimento.",
} as const;

test.describe("Módulo: Pedidos", () => {
  test("03 — Adicionar item ao pedido", async ({ page }) => {

    await configurarTema(page);

    // ── Chega ao carrinho via modo "Sem cadastro" ────────────────────────
    await navegarPara(page, "/app/atendimento/pedido");
    await injetarUITutorial(page);
    await page.waitForSelector("text=Novo Atendimento", { state: "visible", timeout: 15_000 });

    // Seleciona modo "Sem cadastro"
    await page.getByRole("button", { name: /sem cadastro/i }).click();
    await pausaVisual(600);

    const campoNome = page.getByPlaceholder(/Nome do cliente/i);
    await campoNome.type("Visitante Teste", { delay: 60 });
    await pausaVisual(600);
    await page.getByRole("button", { name: /Ir para produtos/i }).click();

    await aguardarCarregamento(page);

    // ── Cena 1: Busca de produto ─────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.busca);
    await pausaVisual(800);

    const campoBusca = page.getByPlaceholder(/Buscar produto/i);
    await campoBusca.waitFor({ state: "visible", timeout: 15_000 });
    // Digita devagar para o vídeo mostrar o filtro em ação
    for (const char of "Peti") {
      await campoBusca.type(char, { delay: 120 });
    }
    await pausaVisual(1_500);
    // Limpa para mostrar catálogo completo
    await campoBusca.fill("");
    await pausaVisual(500);
    await ocultarLegenda(page);

    // ── Cena 2: Adicionar produto ────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.adicionando);
    await pausaVisual(800);

    // Espera pelo menos um produto aparecer no grid
    await page.waitForSelector('button.text-left.rounded-2xl', { timeout: 15_000 });
    const primeiroProduto = page.locator('button.text-left.rounded-2xl').first();
    await primeiroProduto.click();
    await pausaVisual(500);

    // Se modal de adicionais abrir, confirma sem seleção
    const botaoAdicionar = page.getByRole("button", { name: /^Adicionar$/ });
    if (await botaoAdicionar.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await botaoAdicionar.click();
    }

    await pausaVisual(1_000);
    await ocultarLegenda(page);

    // ── Cena 3: Carrinho atualizado ──────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.carrinho);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 4: Botão de pagamento ───────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.pronto);
    await pausaVisual(2_000);
    await ocultarLegenda(page);
  });
});
