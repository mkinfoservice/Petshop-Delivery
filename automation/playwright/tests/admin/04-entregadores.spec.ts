/**
 * Fluxo: Módulo Entregadores
 * Módulo: Administrativo
 * Vídeo: 04-entregadores
 *
 * Roteiro completo em: automation/roteiros/admin/04-entregadores.md
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
    "O módulo Entregadores lista todos os profissionais cadastrados.\nVeja nome, status e rotas ativas de cada um.",
  detalhes:
    "Cada entregador tem perfil com contato e histórico de entregas.\nO status 'Ativo' indica disponibilidade para novas rotas.",
  formulario:
    "Para cadastrar um novo entregador, clique em 'Novo entregador'.\nPreencha nome, telefone e defina as credenciais de acesso.",
} as const;

test.describe("Módulo: Administrativo", () => {
  test("04 — Entregadores", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de entregadores ────────────────────────────────────
    await navegarPara(page, "/app/logistica/entregadores");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Detalhes ─────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.detalhes);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 3: Formulário de novo entregador ────────────────────────────
    await mostrarLegenda(page, LEGENDAS.formulario);
    await pausaVisual(1_000);

    await navegarPara(page, "/app/logistica/entregadores/novo");
    await aguardarCarregamento(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await pausaVisual(2_500);
    await ocultarLegenda(page);
  });
});
