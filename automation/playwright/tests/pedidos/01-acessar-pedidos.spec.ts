/**
 * Fluxo: Acessar módulo Pedidos
 * Módulo: Pedidos
 * Vídeo: 01-acessar-pedidos
 *
 * Roteiro completo em: automation/roteiros/pedidos/01-acessar-pedidos.md
 */

import { test } from "@playwright/test";
import {
  navegarPara,
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
    "A lista de pedidos exibe todos os atendimentos registrados.\nUse as abas para alternar entre Delivery/Balcão e Frente de Caixa.",
  filtros:
    "Filtre por status: Recebido, Em preparo, Pronto, Saiu para entrega e Entregue.\nUse a busca para localizar um pedido pelo número.",
  clique:
    "Clique em qualquer pedido para ver os detalhes\ne gerenciar o status do atendimento.",
} as const;

test.describe("Módulo: Pedidos", () => {
  test("01 — Acessar módulo Pedidos", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de Pedidos ─────────────────────────────────────────
    await navegarPara(page, "/app/pedidos");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Filtros ──────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.filtros);

    const statusSelect = page.locator("select").first();
    await statusSelect.selectOption("EM_PREPARO");
    await pausaVisual(1_200);
    await statusSelect.selectOption("");
    await pausaVisual(600);
    await ocultarLegenda(page);

    // ── Cena 3: Ação ─────────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.clique);
    await pausaVisual(2_500);
    await ocultarLegenda(page);
  });
});
