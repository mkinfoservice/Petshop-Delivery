/**
 * Fluxo: Editar produto
 * Módulo: Produtos
 * Vídeo: 04-editar-produto
 *
 * Roteiro completo em: automation/roteiros/produtos/04-editar-produto.md
 *
 * Setup: busca a primeira categoria disponível e cria produto via API
 * no beforeAll; edita via UI no teste; remove via API no afterAll.
 */

import { test, expect, request } from "@playwright/test";
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
import { clicarBotao } from "../../helpers/form";
import { CREDENCIAIS } from "../../auth/credenciais";

// ── Dados do produto de teste ─────────────────────────────────────────────────
const PRODUTO = {
  nomeOriginal: "Petisco Dental para Cães",
  nomeEditado:  "Petisco Dental Premium para Cães",
};

let produtoId: string | null = null;

const LEGENDAS = {
  ficha:
    "Esta é a tela de edição do produto com todos os dados cadastrados.",
  editando:
    "Altere o campo desejado.\nTodos os dados podem ser modificados aqui.",
  salvando:
    "Clique em Salvar alterações para confirmar.",
  sucesso:
    "Produto atualizado com sucesso!\nAs alterações são refletidas imediatamente no catálogo.",
} as const;

// ── Setup / Teardown via API ──────────────────────────────────────────────────

async function autenticarAPI() {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) return { api, token: null };
  const { token } = await loginRes.json();
  return { api, token: token as string };
}

test.beforeAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }

  const headers = { Authorization: `Bearer ${token}` };

  // Busca categorias públicas para obter um categoryId válido
  const slugEmpresa = new URL(CREDENCIAIS.baseURL).hostname.split(".")[0];
  const catRes = await api.get(`/catalog/${slugEmpresa}/categories`);
  let categoryId: string | null = null;
  if (catRes.ok()) {
    const cats: { id: string; name: string }[] = await catRes.json();
    if (cats.length > 0) categoryId = cats[0].id;
  }

  if (!categoryId) {
    console.warn("[setup] Nenhuma categoria encontrada — produto não será criado.");
    await api.dispose();
    return;
  }

  const createRes = await api.post("/admin/products", {
    headers,
    data: {
      name:       PRODUTO.nomeOriginal,
      categoryId,
      priceCents: 1490,
      costCents:  600,
      stockQty:   10,
      unit:       "UN",
      isActive:   true,
    },
  });

  if (createRes.ok()) {
    const body = await createRes.json();
    produtoId = body.id ?? null;
    console.log(`[setup] Produto de edição criado: ${produtoId}`);
  } else {
    console.warn(`[setup] Falha ao criar produto: ${createRes.status()}`);
  }

  await api.dispose();
});

test.afterAll(async () => {
  if (!produtoId) return;
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }

  await api.delete(`/admin/products/${produtoId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  console.log(`[teardown] Produto de edição removido: ${produtoId}`);
  await api.dispose();
});

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Produtos", () => {
  test("04 — Editar produto", async ({ page }) => {

    test.skip(!produtoId, "Produto de teste não foi criado no beforeAll");

    await configurarTema(page);

    // ── Cena 1: Tela de edição do produto ────────────────────────────────
    await navegarPara(page, `/app/produtos/${produtoId}`);
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.ficha);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Alterar o nome ───────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.editando);
    await pausaVisual(1_000);

    const campoNome = page
      .getByPlaceholder(/Ex: Ração Premium para Cães|nome do produto/i)
      .or(page.getByLabel(/nome/i))
      .first();

    await campoNome.click();
    await campoNome.selectText();
    await campoNome.type(PRODUTO.nomeEditado, { delay: 55 });
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 3: Salvar ───────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.salvando);
    await pausaVisual(800);
    await clicarBotao(page, /salvar alterações|salvar/i as unknown as string);

    // Aguarda retorno para a lista de produtos
    await aguardarCarregamento(page);

    // ── Cena 4: Resultado ────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.sucesso);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Validação ────────────────────────────────────────────────────────
    // Após salvar o form redireciona para /app/produtos (lista)
    await expect(page).toHaveURL(/\/app\/produtos($|\?)/);
  });
});
