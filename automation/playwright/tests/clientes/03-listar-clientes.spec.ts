/**
 * Fluxo: Listar clientes
 * Módulo: Clientes
 * Vídeo: 03-listar-clientes
 *
 * Roteiro completo em: automation/roteiros/clientes/03-listar-clientes.md
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
  listaClientes:
    "A lista de clientes exibe todos os cadastros do sistema.\nCada linha mostra nome, telefone e data de cadastro.",
  rolagem:
    "Use a barra de rolagem ou a paginação para navegar entre os registros.",
  acao:
    "Clique em qualquer cliente para abrir a ficha completa\ncom histórico de pedidos e dados de contato.",
} as const;

test.describe("Módulo: Clientes", () => {
  test("03 — Listar clientes", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de Clientes ────────────────────────────────────────
    await navegarPara(page, "/app/atendimento/clientes");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.listaClientes);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Scroll para mostrar mais registros ───────────────────────
    await mostrarLegenda(page, LEGENDAS.rolagem);
    await page.mouse.wheel(0, 350);
    await pausaVisual(1_500);
    await page.mouse.wheel(0, -350);
    await pausaVisual(600);
    await ocultarLegenda(page);

    // ── Cena 3: Como acessar a ficha ─────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.acao);
    await pausaVisual(2_500);
    await ocultarLegenda(page);
  });
});
