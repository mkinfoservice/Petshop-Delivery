/**
 * Fluxo: Módulo Relatórios
 * Módulo: Administrativo
 * Vídeo: 05-relatorios
 *
 * Roteiro completo em: automation/roteiros/admin/05-relatorios.md
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
  acessando:
    "O módulo Relatórios consolida os dados de vendas e operação.\nAcesse pelo menu lateral em Gestão > Relatórios.",
  tipos:
    "Escolha o tipo de relatório: Vendas, Produtos, Clientes ou Financeiro.\nCada relatório tem filtros de período e agrupamento.",
  filtros:
    "Selecione o período desejado e aplique o filtro.\nOs dados são exibidos em tabela e podem ser exportados.",
} as const;

test.describe("Módulo: Administrativo", () => {
  test("05 — Relatórios", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Acessar relatórios ───────────────────────────────────────
    await navegarPara(page, "/app/relatorios");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.acessando);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Tipos de relatório ───────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.tipos);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 3: Filtros e visualização ───────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.filtros);
    await page.mouse.wheel(0, 300);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
