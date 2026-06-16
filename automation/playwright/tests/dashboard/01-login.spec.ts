/**
 * Fluxo: Login no sistema
 * Módulo: Dashboard / Acesso
 * Vídeo: 01-login
 *
 * Roteiro completo em: automation/roteiros/dashboard/01-login.md
 *
 * IMPORTANTE: este teste sobrescreve o storageState para começar sem
 * sessão autenticada, exibindo o fluxo real de login na gravação.
 */

import { test, expect } from "@playwright/test";
import {
  navegarPara,
  aguardarCarregamento,
  pausaVisual,
  aguardarURL,
} from "../../helpers/navigation";
import {
  configurarTema,
  injetarUITutorial,
  mostrarLegenda,
  ocultarLegenda,
} from "../../helpers/tutorial";
import { CREDENCIAIS } from "../../auth/credenciais";

// ── Legendas do roteiro ───────────────────────────────────────────────────────
const LEGENDAS = {
  telaLogin:
    "Para acessar o vendApps, informe seu usuário e senha.",
  usuario:
    "Digite seu usuário de acesso no campo abaixo.",
  senha:
    "Informe sua senha.\nOs caracteres são ocultados por segurança.",
  entrar:
    "Clique em Entrar para acessar o sistema.",
  sucesso:
    "Acesso realizado com sucesso!\nBem-vindo ao painel principal do vendApps.",
} as const;

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Dashboard", () => {
  // Inicia sem sessão para registrar o fluxo real de login no vídeo
  test.use({ storageState: { cookies: [], origins: [] } });

  test("01 — Login no sistema", async ({ page }) => {

    // ── Setup visual (ANTES do primeiro goto) ────────────────────────────
    await configurarTema(page);

    // ── Cena 1: Tela de login ────────────────────────────────────────────
    await navegarPara(page, "/login");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.telaLogin);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Preencher usuário ────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.usuario);
    await page.locator("#username").click();
    await page.locator("#username").fill(CREDENCIAIS.usuario);
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 3: Preencher senha ──────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.senha);
    await page.locator("#password").click();
    await page.locator("#password").fill(CREDENCIAIS.senha);
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 4: Confirmar login ──────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.entrar);
    await pausaVisual(1_000);
    await page.getByRole("button", { name: /entrar/i }).click();

    // ── Cena 5: Painel principal ─────────────────────────────────────────
    await aguardarURL(page, /\/(app|admin)/, 20_000);
    await aguardarCarregamento(page);

    await mostrarLegenda(page, LEGENDAS.sucesso);
    await pausaVisual(3_500);
    await ocultarLegenda(page);

    // ── Validação ────────────────────────────────────────────────────────
    await expect(page).toHaveURL(/\/(app|admin)/);
  });
});
