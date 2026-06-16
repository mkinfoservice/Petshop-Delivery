import { Page, expect } from "@playwright/test";
import { CREDENCIAIS } from "./credenciais";

/**
 * Realiza login no painel administrativo do vendApps.
 *
 * Usa os IDs reais do formulário (id="username" e id="password")
 * para máxima robustez, independente de alterações de layout.
 *
 * @param page - Instância da página Playwright
 */
export async function realizarLogin(page: Page): Promise<void> {
  await page.goto("/login");
  await page.waitForLoadState("networkidle");

  // Campos identificados por id — mais estáveis que placeholder
  await page.locator("#username").fill(CREDENCIAIS.usuario);
  await page.locator("#password").fill(CREDENCIAIS.senha);

  await page.getByRole("button", { name: /entrar/i }).click();

  // Aguarda redirecionamento para o painel (rota /app ou /admin)
  await page.waitForURL(/\/(app|admin)/, { timeout: 15_000 });

  // Garante que não há mensagem de erro visível
  await expect(page.locator(".text-red-300")).not.toBeVisible({ timeout: 5_000 });
}

/**
 * Verifica se a sessão atual ainda é válida.
 * Útil para testes que precisam reautenticar se o token expirou.
 */
export async function sessaoAtiva(page: Page): Promise<boolean> {
  const token = await page.evaluate(() => localStorage.getItem("vendapps_token"));
  return !!token;
}
