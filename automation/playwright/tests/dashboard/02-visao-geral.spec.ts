/**
 * Fluxo: Dashboard — Visão geral
 * Módulo: Dashboard
 * Vídeo: 02-visao-geral
 *
 * Roteiro completo em: automation/roteiros/dashboard/02-visao-geral.md
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

// ── Legendas do roteiro ───────────────────────────────────────────────────────
const LEGENDAS = {
  painelPrincipal:
    "Este é o painel principal do vendApps.\nAqui você tem acesso a todos os módulos do sistema.",
  menuLateral:
    "O menu lateral organiza os módulos do sistema.\nClique em qualquer seção para acessá-la.",
  atendimento:
    "O módulo de Atendimento reúne os atalhos mais usados no dia a dia:\nNovo cliente, pedidos e consultas.",
  retorno:
    "Você pode retornar ao painel principal a qualquer momento\npelo menu lateral ou pelo logo do sistema.",
} as const;

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Dashboard", () => {
  test("02 — Dashboard — Visão geral", async ({ page }) => {

    // ── Setup visual (ANTES do primeiro goto) ────────────────────────────
    await configurarTema(page);

    // ── Cena 1: Painel principal ─────────────────────────────────────────
    await navegarPara(page, "/app");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.painelPrincipal);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Menu lateral ─────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.menuLateral);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 3: Módulo de Atendimento ────────────────────────────────────
    await navegarPara(page, "/app/atendimento");

    await mostrarLegenda(page, LEGENDAS.atendimento);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 4: Retorno ao painel principal ──────────────────────────────
    await navegarPara(page, "/app");

    await mostrarLegenda(page, LEGENDAS.retorno);
    await pausaVisual(2_500);
    await ocultarLegenda(page);
  });
});
