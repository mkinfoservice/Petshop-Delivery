import { Page } from "@playwright/test";

/**
 * Aguarda a rede estabilizar após uma navegação ou interação.
 * Usa networkidle para garantir que as requisições React Query finalizaram.
 */
export async function aguardarCarregamento(
  page: Page,
  timeout = 15_000
): Promise<void> {
  await page.waitForLoadState("networkidle", { timeout });
}

/**
 * Navega para uma rota relativa e aguarda o carregamento completo.
 * Combina goto + networkidle em um único helper.
 */
export async function navegarPara(page: Page, rota: string): Promise<void> {
  await page.goto(rota);
  await aguardarCarregamento(page);
}

/**
 * Insere uma pausa visual no vídeo — útil para dar tempo ao espectador
 * de ler o que está na tela antes de prosseguir.
 *
 * @param ms - Duração em milissegundos (padrão: 1500ms)
 */
export async function pausaVisual(ms = 1_500): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Aguarda que a URL corresponda ao padrão informado.
 * Útil para confirmar redirecionamentos após submit de formulários.
 */
export async function aguardarURL(
  page: Page,
  padrao: string | RegExp,
  timeout = 15_000
): Promise<void> {
  await page.waitForURL(padrao, { timeout });
}

/**
 * Rola suavemente a página até um elemento ficar visível.
 * Melhora a experiência visual no vídeo do tutorial.
 */
export async function rolarAte(
  page: Page,
  seletor: string
): Promise<void> {
  await page.locator(seletor).scrollIntoViewIfNeeded();
}
