/**
 * Fluxo: Editar cliente
 * Módulo: Clientes
 * Vídeo: 05-editar-cliente
 *
 * Roteiro completo em: automation/roteiros/clientes/05-editar-cliente.md
 *
 * Setup: cria cliente via API no beforeAll, edita via UI no teste,
 * remove via API no afterAll.
 */

import { test, expect, request } from "@playwright/test";
import {
  navegarPara,
  aguardarCarregamento,
  pausaVisual,
  aguardarURL,
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

// ── Dados do cliente ──────────────────────────────────────────────────────────
function gerarTelefone(): string {
  const d = new Date();
  const mm  = String(d.getMonth() + 1).padStart(2, "0");
  const dd  = String(d.getDate()).padStart(2, "0");
  const hh  = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  // "218" (3) + mm(2) + dd(2) + hh(2) + min(2) = 11 dígitos — DDD 21 diferencia de 04-pesquisar
  return `218${mm}${dd}${hh}${min}`;
}

const CLIENTE = {
  nome:            "Carlos Eduardo Lima",
  telefoneOriginal: `(11) 9${new Date().getMinutes()}234-5678`.slice(0, 15),
  telefoneEditado:  `(11) 9${new Date().getMinutes()}876-5432`.slice(0, 15),
};

let clienteId: string | null = null;

const LEGENDAS = {
  ficha:
    "Esta é a ficha do cliente com todos os dados cadastrados.",
  editar:
    "Clique em Editar para alterar os dados do cadastro.",
  alterando:
    "Altere o campo desejado.\nTodos os campos seguem a mesma máscara do cadastro.",
  salvando:
    "Clique em Salvar para confirmar as alterações.",
  sucesso:
    "Dados atualizados com sucesso!\nAs alterações aparecem imediatamente na ficha.",
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
  const createRes = await api.post("/admin/customers", {
    headers,
    data: {
      name:  CLIENTE.nome,
      phone: gerarTelefone(),
    },
  });

  if (createRes.ok()) {
    const body = await createRes.json();
    clienteId = body.id ?? body.customerId ?? null;
    console.log(`[setup] Cliente de edição criado: ${clienteId}`);
  } else {
    console.warn(`[setup] Falha ao criar cliente: ${createRes.status()}`);
  }

  await api.dispose();
});

test.afterAll(async () => {
  if (!clienteId) return;
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }

  await api.delete(`/admin/customers/${clienteId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  console.log(`[teardown] Cliente de edição anonimizado: ${clienteId}`);
  await api.dispose();
});

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Clientes", () => {
  test("05 — Editar cliente", async ({ page }) => {

    // Sem clienteId o teste é ignorado graciosamente
    test.skip(!clienteId, "Cliente de teste não foi criado no beforeAll");

    await configurarTema(page);

    // ── Cena 1: Ficha do cliente ─────────────────────────────────────────
    await navegarPara(page, `/app/atendimento/clientes/${clienteId}`);
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.ficha);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Botão Editar ─────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.editar);
    await pausaVisual(1_000);

    // Tenta encontrar o botão de edição (texto pode variar)
    const btnEditar = page
      .getByRole("button", { name: /editar|edit/i })
      .or(page.getByRole("link", { name: /editar|edit/i }))
      .first();

    await btnEditar.click();
    await aguardarCarregamento(page);
    await ocultarLegenda(page);

    // ── Cena 3: Alterar um campo ─────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.alterando);
    await pausaVisual(1_000);

    // Limpa e redigita o nome para demonstrar a edição
    const campoNome = page
      .getByPlaceholder(/nome completo|nome/i)
      .or(page.getByLabel(/nome/i))
      .first();

    await campoNome.click();
    await campoNome.selectText();
    await campoNome.type(CLIENTE.nome, { delay: 60 });
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 4: Salvar ───────────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.salvando);
    await pausaVisual(800);
    await clicarBotao(page, /salvar|atualizar|confirmar/i as unknown as string);

    // ── Cena 5: Resultado ────────────────────────────────────────────────
    // Aguarda voltar para ficha ou atualizar
    await aguardarCarregamento(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.sucesso);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Validação ────────────────────────────────────────────────────────
    await expect(page.locator("h1")).toContainText(CLIENTE.nome.split(" ")[0]);
  });
});
