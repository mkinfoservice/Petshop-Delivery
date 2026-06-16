/**
 * Fluxo: Módulo Financeiro
 * Módulo: Administrativo
 * Vídeo: 02-financeiro
 *
 * Roteiro completo em: automation/roteiros/admin/02-financeiro.md
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
  visaoGeral:
    "O módulo Financeiro mostra o resumo de receitas e despesas do período.\nAcompanhe o saldo operacional em tempo real.",
  lancamentos:
    "Acesse Lançamentos para ver todas as entradas e saídas registradas.\nFiltre por período, categoria ou tipo.",
  filtros:
    "Use os filtros para analisar períodos específicos\ne exportar relatórios financeiros.",
} as const;

test.describe("Módulo: Administrativo", () => {
  test("02 — Financeiro", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Visão geral financeira ───────────────────────────────────
    await navegarPara(page, "/app/financeiro");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.visaoGeral);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Lançamentos ──────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.lancamentos);
    await pausaVisual(1_000);

    await navegarPara(page, "/app/financeiro/lancamentos");
    await aguardarCarregamento(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // ── Cena 3: Filtros ──────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.filtros);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
