/**
 * Fluxo: Acessar módulo Produtos
 * Módulo: Produtos
 * Vídeo: 01-acessar-produtos
 *
 * Roteiro completo em: automation/roteiros/produtos/01-acessar-produtos.md
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
  dashboard:
    "Você está no Dashboard principal do vendApps.\nAqui você tem uma visão geral do negócio.",
  navegando:
    "Para acessar o catálogo de produtos, clique em Produtos no menu lateral.",
  produtos:
    "Bem-vindo ao módulo de Produtos!\nAqui você gerencia todo o catálogo da sua empresa.",
} as const;

test.describe("Módulo: Produtos", () => {
  test("01 — Acessar módulo Produtos", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Dashboard ────────────────────────────────────────────────
    await navegarPara(page, "/app");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.dashboard);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Navegar para Produtos ────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.navegando);
    await pausaVisual(1_500);

    await navegarPara(page, "/app/produtos");
    await ocultarLegenda(page);

    // ── Cena 3: Lista de Produtos ────────────────────────────────────────
    await injetarUITutorial(page);
    await mostrarLegenda(page, LEGENDAS.produtos);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
