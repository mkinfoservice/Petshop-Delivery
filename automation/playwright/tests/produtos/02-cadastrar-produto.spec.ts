/**
 * Fluxo: Cadastrar produto
 * Módulo: Produtos
 * Vídeo: 02-cadastrar-produto
 *
 * Roteiro completo em: automation/roteiros/produtos/02-cadastrar-produto.md
 *
 * Setup: cria produto via UI no teste; captura o ID do redirect e
 * remove via API no afterAll.
 */

import { test, expect, request } from "@playwright/test";
import {
  navegarPara,
  aguardarCarregamento,
  pausaVisual,
} from "../../helpers/navigation";
import {
  preencherPorPlaceholder,
  clicarBotao,
} from "../../helpers/form";
import {
  configurarTema,
  injetarUITutorial,
  mostrarLegenda,
  ocultarLegenda,
} from "../../helpers/tutorial";
import { CREDENCIAIS } from "../../auth/credenciais";

// ── Dados do produto ───────────────────────────────────────────────────────────
const PRODUTO = {
  nome:  "Ração Premium para Cães",
  preco: "25,90",
};

// ID capturado a partir do redirect após a criação
let produtoIdCriado: string | null = null;

const LEGENDAS = {
  formulario:
    "Preencha os dados do produto.\nCampos marcados com * são obrigatórios.",
  nome:
    "Digite o nome do produto.\nO sistema gera o slug (URL) automaticamente.",
  categoria:
    "Selecione a categoria do produto.\nEla organiza o catálogo para os clientes.",
  preco:
    "Informe o preço de venda e o custo.\nO sistema calcula a margem em tempo real.",
  salvando:
    "Clique em Criar produto para finalizar o cadastro.",
  sucesso:
    "Produto cadastrado com sucesso!\nVocê é redirecionado para a ficha de edição.",
} as const;

// ── Setup / Teardown via API ──────────────────────────────────────────────────

async function autenticarAPI() {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) return { api, token: null as null };
  const { token } = await loginRes.json();
  return { api, token: token as string };
}

// Remove qualquer produto residual de runs anteriores com o mesmo nome
// (evita conflito de slug único que impede a criação via UI)
test.beforeAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  const headers = { Authorization: `Bearer ${token}` };

  const res = await api.get(
    `/admin/products?search=${encodeURIComponent(PRODUTO.nome)}&pageSize=20`,
    { headers }
  );
  if (res.ok()) {
    const body = await res.json();
    for (const item of body.items ?? []) {
      if (item.name === PRODUTO.nome) {
        await api.delete(`/admin/products/${item.id}`, { headers });
        console.log(`[setup] Produto residual removido: ${item.id}`);
      }
    }
  }
  await api.dispose();
});

test.afterAll(async () => {
  if (!produtoIdCriado) return;

  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }

  await api.delete(`/admin/products/${produtoIdCriado}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  console.log(`[teardown] Produto de demonstração removido: ${produtoIdCriado}`);
  await api.dispose();
});

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Produtos", () => {
  test("02 — Cadastrar produto", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Formulário de cadastro ───────────────────────────────────
    await navegarPara(page, "/app/produtos/new");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.formulario);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // ── Cena 2: Nome do produto ──────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.nome);
    await pausaVisual(800);

    await preencherPorPlaceholder(
      page,
      /Ex: Ração Premium para Cães|nome do produto/i,
      PRODUTO.nome,
      { delay: 55 }
    );
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 3: Categoria ────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.categoria);
    await pausaVisual(800);

    // Aguarda as opções de categoria carregarem (busca assíncrona)
    const categoriaSelect = page.locator("select[required]").first();
    await categoriaSelect.waitFor({ state: "visible", timeout: 15_000 });
    await page.waitForFunction(
      () => {
        const sel = document.querySelector("select[required]");
        return sel instanceof HTMLSelectElement && sel.options.length > 1;
      },
      { timeout: 15_000 }
    );
    await categoriaSelect.selectOption({ index: 1 });
    await pausaVisual(1_000);
    await ocultarLegenda(page);

    // ── Cena 4: Preço ────────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.preco);
    await pausaVisual(800);

    const campoPreco = page.getByPlaceholder("0,00").first();
    await campoPreco.click();
    await campoPreco.selectText();
    await campoPreco.type(PRODUTO.preco, { delay: 80 });
    await pausaVisual(1_500);
    await ocultarLegenda(page);

    // ── Cena 5: Salvar ───────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.salvando);
    await pausaVisual(800);
    await clicarBotao(page, /criar produto/i as unknown as string);

    // Aguarda redirect para a ficha do produto recém-criado
    // (exclui /new para não dar match falso quando o form rejeita o submit)
    await page.waitForURL(
      (url) => url.pathname.startsWith("/app/produtos/") && url.pathname !== "/app/produtos/new",
      { timeout: 20_000 }
    );
    await aguardarCarregamento(page);

    // Captura o ID a partir da URL para o teardown
    const url = page.url();
    const idDaURL = url.split("/").pop();
    if (idDaURL && idDaURL !== "new") {
      produtoIdCriado = idDaURL;
      console.log(`[setup] Produto de demonstração criado: ${produtoIdCriado}`);
    }

    // ── Cena 6: Resultado ────────────────────────────────────────────────
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.sucesso);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Validação ────────────────────────────────────────────────────────
    await expect(page.locator("h1")).toContainText(/editar produto/i);
  });
});
