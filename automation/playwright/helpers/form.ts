import { Page, Locator } from "@playwright/test";

/**
 * Preenche um campo de formulário pelo placeholder.
 *
 * Clica no campo antes de preencher para garantir foco visível no vídeo.
 * Usa fill() que substitui o conteúdo inteiro (mais confiável que type()).
 */
export async function preencherPorPlaceholder(
  page: Page,
  placeholder: string,
  valor: string
): Promise<void> {
  const campo = page.getByPlaceholder(placeholder);
  await campo.click();
  await campo.fill(valor);
}

/**
 * Preenche um campo de formulário pelo label visível.
 *
 * Funciona quando o <label> está corretamente associado ao input via htmlFor/id.
 * No vendApps, o CustomerForm usa componente <Field> sem htmlFor — usar
 * preencherPorPlaceholder() nesses casos.
 */
export async function preencherPorLabel(
  page: Page,
  label: string,
  valor: string
): Promise<void> {
  await page.getByLabel(label).click();
  await page.getByLabel(label).fill(valor);
}

/**
 * Clica em um botão pelo texto (case-insensitive, parcial).
 *
 * @example clicarBotao(page, 'Cadastrar cliente')
 */
export async function clicarBotao(
  page: Page,
  texto: string
): Promise<void> {
  await page.getByRole("button", { name: new RegExp(texto, "i") }).click();
}

/**
 * Limpa um campo e digita um novo valor.
 * Útil quando fill() não dispara os handlers de máscara (onChange).
 */
export async function digitarCampo(
  page: Page,
  locator: Locator,
  valor: string
): Promise<void> {
  await locator.click();
  await locator.selectText();
  await locator.type(valor, { delay: 80 });
}

/**
 * Verifica se um botão está habilitado (sem disabled attribute).
 */
export async function botaoHabilitado(
  page: Page,
  texto: string
): Promise<boolean> {
  const btn = page.getByRole("button", { name: new RegExp(texto, "i") });
  return !(await btn.isDisabled());
}
