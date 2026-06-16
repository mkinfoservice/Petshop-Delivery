/**
 * Fluxo: Módulo Rotas de entrega
 * Módulo: Administrativo
 * Vídeo: 03-rotas
 *
 * Roteiro completo em: automation/roteiros/admin/03-rotas.md
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
    "O módulo Rotas organiza as entregas em percursos otimizados.\nAqui você vê todas as rotas criadas e seus status.",
  status:
    "Cada rota passa pelos status: Criada → Atribuída → Em andamento → Concluída.\nFiltre para acompanhar rotas em tempo real.",
  planner:
    "O planejador agrupa pedidos prontos em rotas automáticas.\nAcesse pelo botão 'Planejar rotas' no topo da lista.",
} as const;

test.describe("Módulo: Administrativo", () => {
  test("03 — Rotas de entrega", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de rotas ───────────────────────────────────────────
    await navegarPara(page, "/app/logistica/rotas");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Status das rotas ─────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.status);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 3: Planejador ───────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.planner);
    await pausaVisual(1_000);

    await navegarPara(page, "/app/logistica/rotas/planner");
    await aguardarCarregamento(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await pausaVisual(2_500);
    await ocultarLegenda(page);
  });
});
