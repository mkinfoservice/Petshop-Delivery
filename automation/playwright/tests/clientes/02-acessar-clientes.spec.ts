/**
 * Fluxo: Acessar módulo Clientes
 * Módulo: Clientes
 * Vídeo: 02-acessar-clientes
 *
 * Roteiro completo em: automation/roteiros/clientes/02-acessar-clientes.md
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
  dashboard:
    "Partindo do painel principal, acesse o módulo de Atendimento.",
  atendimento:
    "No hub de Atendimento você encontra os atalhos principais.\nClique em Clientes para ver a lista.",
  listaClientes:
    "Esta é a lista de clientes cadastrados no sistema.\nVocê pode buscar, filtrar e acessar a ficha de cada um.",
} as const;

test.describe("Módulo: Clientes", () => {
  test("02 — Acessar módulo Clientes", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Dashboard ────────────────────────────────────────────────
    await navegarPara(page, "/app");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.dashboard);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Hub de Atendimento ───────────────────────────────────────
    await navegarPara(page, "/app/atendimento");

    await mostrarLegenda(page, LEGENDAS.atendimento);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 3: Lista de Clientes ────────────────────────────────────────
    await navegarPara(page, "/app/atendimento/clientes");

    await mostrarLegenda(page, LEGENDAS.listaClientes);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
