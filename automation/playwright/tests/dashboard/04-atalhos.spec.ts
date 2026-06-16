/**
 * Fluxo: Dashboard — Atalhos rápidos
 * Módulo: Dashboard
 * Vídeo: 04-atalhos
 *
 * Roteiro completo em: automation/roteiros/dashboard/04-atalhos.md
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
  botaoAtendimento:
    "O botão 'Montar Pedido' no topo do painel\nabre diretamente o fluxo de atendimento.",
  fluxoAtendimento:
    "Ao clicar, você acessa o PhoneOrderBuilder\npronto para buscar o cliente e montar o pedido.",
  retorno:
    "Retorne ao painel pelo menu lateral a qualquer momento.",
  cardsAtalho:
    "Os cards de status também são atalhos:\nclicar em 'Recebidos' abre a lista de pedidos filtrada por esse status.",
} as const;

test.describe("Módulo: Dashboard", () => {
  test("04 — Dashboard — Atalhos rápidos", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Dashboard com botão em destaque ──────────────────────────
    await navegarPara(page, "/app/dashboard");
    await injetarUITutorial(page);

    await page.waitForSelector("text=Montar Pedido", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.botaoAtendimento);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Clique em Montar Pedido ──────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.fluxoAtendimento);
    await pausaVisual(800);

    await page.getByRole("button", { name: /Montar Pedido/i }).click();
    await aguardarCarregamento(page);
    await page.waitForURL(/\/atendimento\/pedido/, { timeout: 15_000 });

    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 3: Retorno ao dashboard ─────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.retorno);
    await pausaVisual(800);

    await navegarPara(page, "/app/dashboard");
    await page.waitForSelector("text=Pedidos", { state: "visible", timeout: 15_000 });

    await pausaVisual(1_500);
    await ocultarLegenda(page);

    // ── Cena 4: Cards clicáveis de pedidos ───────────────────────────────
    await mostrarLegenda(page, LEGENDAS.cardsAtalho);
    await pausaVisual(1_200);

    // Clica no card "Recebidos" (primeiro card clicável da seção Pedidos)
    const cardRecebidos = page.getByRole("button", { name: /Recebidos/i }).first();
    await cardRecebidos.click();
    await aguardarCarregamento(page);
    await page.waitForURL(/\/pedidos/, { timeout: 15_000 });

    await pausaVisual(2_500);
    await ocultarLegenda(page);
  });
});
