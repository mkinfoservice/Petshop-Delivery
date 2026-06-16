/**
 * Fluxo: Cadastrar novo cliente a partir do dashboard
 * Módulo: Clientes
 * Vídeo: 01-cadastrar-cliente
 *
 * Roteiro completo em: automation/roteiros/clientes/01-cadastrar-cliente.md
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

// ── Dados do cliente de teste ────────────────────────────────────────────────
// Telefone e CPF são gerados dinamicamente para evitar conflito de unicidade
// entre execuções. O nome permanece fixo para manter a consistência do tutorial.

/** Telefone único por execução: (11) 9MMDD-HHMM */
function gerarTelefone(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  const hh = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  return `(11) 9${mm}${dd}-${hh}${min}`;
}

/** CPF válido gerado algoritmicamente (dígitos verificadores corretos) */
function gerarCpf(): string {
  const d = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  // Garante que não são todos iguais (CPFs como 111.111.111-11 são inválidos)
  if (d.every((n) => n === d[0])) d[0] = (d[0] + 1) % 10;

  const c1 = d.reduce((s, v, i) => s + v * (10 - i), 0) % 11;
  const dig1 = c1 < 2 ? 0 : 11 - c1;

  const c2 = [...d, dig1].reduce((s, v, i) => s + v * (11 - i), 0) % 11;
  const dig2 = c2 < 2 ? 0 : 11 - c2;

  const cpf = [...d, dig1, dig2].join("");
  return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`;
}

const CLIENTE = {
  nome: "Ana Paula Rodrigues",
  telefone: gerarTelefone(), // ex: "(11) 90415-1946"
  cpf: gerarCpf(),           // ex: "847.291.630-05"
};

// ── Legendas do roteiro (cena a cena) ────────────────────────────────────────
const LEGENDAS = {
  dashboard:
    "Este é o painel principal do vendApps.\nAcesse o módulo de Atendimento para cadastrar um novo cliente.",
  atendimento:
    "O módulo de Atendimento reúne os atalhos mais usados.\nClique em Novo cliente.",
  formulario:
    "Formulário de cadastro — apenas o Nome é obrigatório.\nTelefone e CPF são preenchidos com máscara automática.",
  preenchendoNome:
    "Digite o nome completo do cliente.",
  preenchendoTelefone:
    "O telefone aceita 10 ou 11 dígitos e formata automaticamente.",
  preenchendoCpf:
    "O CPF é validado pelos dígitos verificadores ao confirmar.",
  confirmando:
    "Clique em Cadastrar cliente para salvar.",
  sucesso:
    "Cliente cadastrado com sucesso!\nO sistema redireciona automaticamente para a ficha do cliente.",
} as const;

// ── Teardown: anonimiza o cliente de teste após a gravação ────────────────────
// Permite regravar o vídeo quantas vezes for necessário sem conflito de telefone.
// Usa a API do backend diretamente (não o frontend).
test.afterAll(async () => {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });

  // Autentica na API do backend
  const loginRes = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!loginRes.ok()) {
    console.warn(`[teardown] Login falhou: ${loginRes.status()}`);
    return;
  }

  const { token } = await loginRes.json();
  const headers = { Authorization: `Bearer ${token}` };

  // Busca o cliente pelo telefone (sem máscara)
  const phoneDigits = CLIENTE.telefone.replace(/\D/g, "");
  const searchRes = await api.get(
    `/admin/customers?phone=${phoneDigits}&pageSize=5`,
    { headers }
  );
  if (!searchRes.ok()) return;

  const body = await searchRes.json();
  const items: Array<{ id: string; name: string }> = body.items ?? body ?? [];

  for (const c of items) {
    if (c.name === CLIENTE.nome) {
      // Troca o telefone por um placeholder neutro (o backend ignora phone:null)
      // Isso libera o número único para a próxima execução
      await api.put(`/admin/customers/${c.id}`, {
        headers,
        data: { name: "Teste removido", phone: "00000000000" },
      });
      console.log(`[teardown] Telefone de teste liberado para: ${c.id}`);
    }
  }

  await api.dispose();
});

// ── Teste ────────────────────────────────────────────────────────────────────

test.describe("Módulo: Clientes", () => {
  test("01 — Cadastrar novo cliente a partir do dashboard", async ({ page }) => {

    // ── Setup visual (ANTES do primeiro goto) ────────────────────────────────
    // Força light mode: addInitScript sobrescreve localStorage antes do React montar
    await configurarTema(page);

    // ── Cena 1: Dashboard ────────────────────────────────────────────────────
    await navegarPara(page, "/app");
    await injetarUITutorial(page); // injeta cursor + sistema de legendas (uma vez por teste)

    await mostrarLegenda(page, LEGENDAS.dashboard);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 2: Hub de Atendimento ───────────────────────────────────────────
    await navegarPara(page, "/app/atendimento");
    await mostrarLegenda(page, LEGENDAS.atendimento);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 3: Formulário de novo cliente ───────────────────────────────────
    await page.getByRole("link", { name: /novo cliente/i }).click();
    await aguardarCarregamento(page);
    await expect(page).toHaveURL(/\/clientes\/novo$/);

    await mostrarLegenda(page, LEGENDAS.formulario);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 4a: Preencher Nome ──────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.preenchendoNome);
    await preencherPorPlaceholder(page, "Nome completo", CLIENTE.nome);
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 4b: Preencher Telefone ──────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.preenchendoTelefone);
    await preencherPorPlaceholder(page, "(21) 99999-9999", CLIENTE.telefone);
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 4c: Preencher CPF ───────────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.preenchendoCpf);
    await preencherPorPlaceholder(page, "000.000.000-00", CLIENTE.cpf);
    await pausaVisual(1_200);
    await ocultarLegenda(page);

    // ── Cena 5: Confirmar cadastro ───────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.confirmando);
    await pausaVisual(1_500); // espectador vê os dados antes do clique
    await clicarBotao(page, "Cadastrar cliente");

    // ── Cena 6: Resultado ────────────────────────────────────────────────────
    // Padrão UUID (8 hex + hífen) garante que /novo não casa por engano
    await aguardarURL(page, /\/clientes\/[a-f0-9]{8}-/, 20_000);
    // Aguarda o <h1> carregar (CustomerDetail busca os dados via API)
    await page.waitForSelector("h1", { state: "visible", timeout: 20_000 });

    await mostrarLegenda(page, LEGENDAS.sucesso);
    await pausaVisual(3_500);

    // ── Validações ───────────────────────────────────────────────────────────
    await expect(page.locator("h1")).toContainText(CLIENTE.nome);
    await expect(page.locator(".text-red-600")).not.toBeVisible();

    await ocultarLegenda(page);
  });
});
