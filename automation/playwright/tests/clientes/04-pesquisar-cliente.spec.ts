/**
 * Fluxo: Pesquisar cliente
 * Módulo: Clientes
 * Vídeo: 04-pesquisar-cliente
 *
 * Roteiro completo em: automation/roteiros/clientes/04-pesquisar-cliente.md
 *
 * Setup: cria um cliente via API antes do teste para garantir resultado
 * na busca. Remove-o via API no teardown.
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
import { CREDENCIAIS } from "../../auth/credenciais";

// ── Dados do cliente de busca ─────────────────────────────────────────────────
function gerarTelefone(): string {
  const d = new Date();
  const mm  = String(d.getMonth() + 1).padStart(2, "0");
  const dd  = String(d.getDate()).padStart(2, "0");
  const hh  = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  // "119" (3) + mm(2) + dd(2) + hh(2) + min(2) = 11 dígitos exatos — precisão de minuto
  return `119${mm}${dd}${hh}${min}`;
}

const CLIENTE = {
  nome:     "Beatriz Santos",
  telefone: gerarTelefone(),
};

// ID criado no beforeAll, usado no afterAll
let clienteId: string | null = null;

const LEGENDAS = {
  lista:
    "Na lista de clientes, use o campo de busca para localizar um registro.",
  digitando:
    "Digite o nome ou parte do telefone do cliente.\nO sistema filtra os resultados em tempo real.",
  resultado:
    "Os clientes que correspondem à busca aparecem imediatamente.\nClique no nome para abrir a ficha.",
  ficha:
    "Ficha completa do cliente localizado pela pesquisa.",
} as const;

// ── Setup / Teardown via API ──────────────────────────────────────────────────

test.beforeAll(async () => {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });

  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) { await api.dispose(); return; }

  const { token } = await loginRes.json();
  const headers = { Authorization: `Bearer ${token}` };

  const createRes = await api.post("/admin/customers", {
    headers,
    data: { name: CLIENTE.nome, phone: CLIENTE.telefone },
  });
  if (createRes.ok()) {
    const body = await createRes.json();
    clienteId = body.id ?? body.customerId ?? null;
  }

  await api.dispose();
});

test.afterAll(async () => {
  if (!clienteId) return;
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });

  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) { await api.dispose(); return; }

  const { token } = await loginRes.json();
  await api.delete(`/admin/customers/${clienteId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  await api.dispose();
});

// ── Teste ─────────────────────────────────────────────────────────────────────

test.describe("Módulo: Clientes", () => {
  test("04 — Pesquisar cliente", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Lista de Clientes ────────────────────────────────────────
    await navegarPara(page, "/app/atendimento/clientes");
    await injetarUITutorial(page);

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // ── Cena 2: Usar o campo de busca ────────────────────────────────────
    const campoBusca = page
      .getByRole("searchbox")
      .or(page.getByPlaceholder(/buscar|pesquisar|search|cliente|nome/i))
      .first();

    await mostrarLegenda(page, LEGENDAS.digitando);
    await campoBusca.click();
    // Digita devagar para o espectador ver as letras aparecerem
    for (const char of CLIENTE.nome.slice(0, 7)) {
      await campoBusca.type(char, { delay: 120 });
    }
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // ── Cena 3: Resultado filtrado ───────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.resultado);
    await pausaVisual(2_000);

    // Clica no primeiro link que contém o texto pesquisado
    const linkResultado = page
      .locator("a")
      .filter({ hasText: new RegExp(CLIENTE.nome.split(" ")[0], "i") })
      .first();

    const linkVisivel = await linkResultado.isVisible({ timeout: 5_000 }).catch(() => false);
    if (linkVisivel) {
      await ocultarLegenda(page);
      await linkResultado.click();
      await aguardarCarregamento(page);
      await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

      // ── Cena 4: Ficha ──────────────────────────────────────────────────
      await mostrarLegenda(page, LEGENDAS.ficha);
      await pausaVisual(3_000);
      await ocultarLegenda(page);

      await expect(page.locator("h1")).toContainText(CLIENTE.nome.split(" ")[0]);
    } else {
      // Fallback: encerra na tela de busca mesmo sem clicar
      await ocultarLegenda(page);
      await pausaVisual(1_000);
    }
  });
});
