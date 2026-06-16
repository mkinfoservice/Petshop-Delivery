/**
 * Global Setup de Autenticação — executa UMA VEZ antes de todos os testes.
 *
 * Fluxo:
 *  1. Abre o Chromium sem estado de sessão
 *  2. Faz login com as credenciais configuradas
 *  3. Salva cookies + localStorage em auth/storage-state.json
 *
 * Todos os testes carregam esse arquivo via `storageState` no config,
 * começando já autenticados sem repetir o fluxo de login.
 *
 * Formato: globalSetup (exporta função default, não usa test())
 */
import { chromium, FullConfig } from "@playwright/test";
import path from "path";
import { CREDENCIAIS } from "./credenciais";

const AUTH_FILE = path.join(__dirname, "storage-state.json");

async function globalSetup(_config: FullConfig): Promise<void> {
  const browser = await chromium.launch();
  const context = await browser.newContext({
    baseURL: CREDENCIAIS.baseURL,
    locale: "pt-BR",
    timezoneId: "America/Sao_Paulo",
  });
  const page = await context.newPage();

  console.log("\n[setup] Realizando login em", CREDENCIAIS.baseURL);

  await page.goto("/login");
  await page.waitForLoadState("networkidle");

  await page.locator("#username").fill(CREDENCIAIS.usuario);
  await page.locator("#password").fill(CREDENCIAIS.senha);
  await page.getByRole("button", { name: /entrar/i }).click();

  // Aguarda redirecionamento para o painel
  await page.waitForURL(/\/(app|admin)/, { timeout: 20_000 });

  // Salva o estado da sessão para ser reutilizado pelos testes
  await context.storageState({ path: AUTH_FILE });
  await browser.close();

  console.log(`[setup] Sessão salva em: ${AUTH_FILE}\n`);
}

export default globalSetup;
