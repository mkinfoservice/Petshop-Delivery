/**
 * Fluxo: Listar produtos
 * Módulo: Produtos
 * Vídeo: 03-listar-produtos
 *
 * Roteiro completo em: automation/roteiros/produtos/03-listar-produtos.md
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
    "A lista de produtos exibe todo o catálogo cadastrado.\nCada linha mostra nome, categoria, preço e margem.",
  filtros:
    "Use os filtros de status e a barra de busca\npara localizar produtos rapidamente.",
  rolagem:
    "Role a lista para visualizar todos os produtos.\nClique em qualquer linha para abrir o cadastro.",
} as const;

test.describe("Módulo: Produtos", () => {
  test("03 — Listar produtos", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de Produtos ────────────────────────────────────────
    await navegarPara(page, "/app/produtos");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Filtros e busca ──────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.filtros);

    const campoBusca = page
      .getByPlaceholder(/buscar|nome|codigo/i)
      .first();

    await campoBusca.click();
    for (const char of "Racao") {
      await campoBusca.type(char, { delay: 100 });
    }
    await pausaVisual(1_500);
    // Limpa a busca para mostrar lista completa novamente
    await campoBusca.fill("");
    await pausaVisual(800);
    await ocultarLegenda(page);

    // ── Cena 3: Rolagem ──────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.rolagem);
    await page.mouse.wheel(0, 350);
    await pausaVisual(1_500);
    await page.mouse.wheel(0, -350);
    await pausaVisual(600);
    await ocultarLegenda(page);
  });
});
