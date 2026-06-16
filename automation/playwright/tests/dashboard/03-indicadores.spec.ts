/**
 * Fluxo: Dashboard — Indicadores operacionais
 * Módulo: Dashboard
 * Vídeo: 03-indicadores
 *
 * Roteiro completo em: automation/roteiros/dashboard/03-indicadores.md
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
  visaoGeral:
    "O painel principal exibe os indicadores operacionais em tempo real.\nAcompanhe pedidos, rotas e entregadores de uma só tela.",
  pedidos:
    "A seção Pedidos mostra o volume por status:\nRecebidos, Em preparo, Prontos, Saiu p/ entrega, Entregues e Cancelados.",
  rotas:
    "A seção Rotas mostra o andamento das entregas:\nCriadas, Atribuídas, Em andamento, Concluídas e Canceladas.",
  entregadores:
    "A seção Entregadores mostra o time disponível:\nTotal cadastrado, ativos no momento e quantos estão com rota ativa.",
} as const;

test.describe("Módulo: Dashboard", () => {
  test("03 — Dashboard — Indicadores operacionais", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Dashboard carregado ──────────────────────────────────────
    await navegarPara(page, "/app/dashboard");
    await injetarUITutorial(page);

    // Aguarda os dados do dashboard carregarem (pelo menos uma stat card)
    await page.waitForSelector("text=Pedidos", { state: "visible", timeout: 20_000 });

    await mostrarLegenda(page, LEGENDAS.visaoGeral);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Seção Pedidos ────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.pedidos);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 3: Seção Rotas ──────────────────────────────────────────────
    // Rola suavemente até a seção de Rotas
    await page.mouse.wheel(0, 400);
    await pausaVisual(600);

    await mostrarLegenda(page, LEGENDAS.rotas);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 4: Seção Entregadores ───────────────────────────────────────
    await page.mouse.wheel(0, 350);
    await pausaVisual(600);

    await mostrarLegenda(page, LEGENDAS.entregadores);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
