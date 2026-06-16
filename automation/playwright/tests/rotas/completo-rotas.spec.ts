/**
 * Fluxo: Fluxo completo de rotas
 * Módulo: Logística — Rotas
 * Vídeo: completo_rotas (~3 min)
 *
 * Roteiro: automation/roteiros/rotas/completo-rotas.md
 *
 * Cobre todo o ciclo de vida de uma rota de entrega:
 *   1. Admin: lista de rotas → planejador → criar rota com 3 pedidos
 *   2. Admin: detalhe da rota recém-criada
 *   3. Motoboy: login → home → iniciar rota
 *   4. Motoboy: processar paradas (Entregue / Pular / Falhou)
 *   5. Notificações WhatsApp simuladas após cada ação
 *   6. Admin: rota concluída com resumo das paradas
 *
 * Setup:  cria 3 pedidos via API e avança para status pronto-para-entrega.
 * Teardown: cancela pedidos de teste e deleta a rota criada.
 */

import { test, expect, request } from "@playwright/test";
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
  mostrarOverlayWhatsApp,
} from "../../helpers/tutorial";
import { CREDENCIAIS } from "../../auth/credenciais";

// ── Dados globais ──────────────────────────────────────────────────────────────

let routeId: string | null = null;
const orderIds: string[] = [];
const customerIds: string[] = [];

const CLIENTES_TESTE = [
  { nome: "Ana Lima",    telefone: "21991110001" },
  { nome: "Bruno Mota",  telefone: "21991110002" },
  { nome: "Carla Nunes", telefone: "21991110003" },
];

const LEGENDAS = {
  lista:
    "O módulo Rotas organiza as entregas em percursos otimizados.\nAcompanhe o status de cada rota pela lista.",
  planner:
    "O planejador agrupa pedidos prontos para entrega.\nSelecione o entregador e os pedidos para calcular o percurso.",
  selecionar:
    "Marque os pedidos que farão parte desta rota.\nO planejador calcula automaticamente a sequência ideal.",
  preview:
    "O planejador oferece dois percursos otimizados — Rota A e Rota B.\nEscolha o que melhor se encaixa na sua operação.",
  detalhe:
    "Rota criada! Todas as paradas aparecem com status Pendente.\nO entregador já pode visualizar no aplicativo.",
  motoBoyHome:
    "No aplicativo do entregador, a rota atribuída aparece com o próximo endereço.\nClique em Ver Rota para abrir o percurso completo.",
  iniciarRota:
    "Ao iniciar a rota, o sistema registra o horário e atualiza o painel do administrador.",
  parada1:
    "Confirme a entrega ao chegar no endereço.\nO cliente recebe notificação automática no WhatsApp.",
  parada2:
    "Se necessário, pule uma parada e informe o motivo.\nVocê pode retornar a ela depois de concluir as demais.",
  parada3:
    "Em caso de insucesso, registre o motivo da falha.\nO administrador é informado e pode reagendar.",
  concluida:
    "Rota finalizada! Todas as paradas foram processadas.\nO histórico fica registrado para relatórios.",
  adminFinal:
    "O administrador visualiza o resultado completo:\nEntregue, Pulada e Falha — com os motivos registrados.",
} as const;

// ── Helpers de API ─────────────────────────────────────────────────────────────

async function autenticarAPI() {
  const api = await request.newContext({ baseURL: CREDENCIAIS.apiURL });
  const res = await api.post("/auth/login", {
    data: { username: CREDENCIAIS.usuario, password: CREDENCIAIS.senha },
  });
  if (!res.ok()) return { api, token: null as null };
  const { token } = await res.json();
  return { api, token: token as string };
}

// ── Setup ──────────────────────────────────────────────────────────────────────

test.beforeAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  const headers = { Authorization: `Bearer ${token}` };

  // Busca um produto ativo para montar os pedidos
  const prodRes = await api.get("/admin/products?active=true&pageSize=1", { headers });
  let productId: string | null = null;
  if (prodRes.ok()) {
    const body = await prodRes.json();
    productId = body.items?.[0]?.id ?? null;
  }

  if (!productId) {
    console.warn("[setup] Nenhum produto ativo encontrado — pedidos não serão criados.");
    await api.dispose();
    return;
  }

  // Endereços com coordenadas GPS no bairro Bangu (< 2km do depot).
  // Depot: -22.8785, -43.4695 | Raio: 11km
  const ENDERECOS = [
    { street: "Rua Bangu",             number: "500", neighborhood: "Bangu",     city: "Rio de Janeiro", state: "RJ", cep: "21851-050", lat: -22.882, lng: -43.468 },
    { street: "Rua Conselheiro Galvão", number: "100", neighborhood: "Realengo", city: "Rio de Janeiro", state: "RJ", cep: "21710-020", lat: -22.869, lng: -43.448 },
    { street: "Rua Padre Miguel",       number: "200", neighborhood: "Padre Miguel", city: "Rio de Janeiro", state: "RJ", cep: "21720-010", lat: -22.876, lng: -43.460 },
  ];

  // Cria 3 clientes + 3 pedidos de ENTREGA com endereço
  for (let i = 0; i < CLIENTES_TESTE.length; i++) {
    const cliente  = CLIENTES_TESTE[i];
    const endereco = ENDERECOS[i];

    // Criar (ou atualizar) cliente COM coordenadas GPS — o pedido herda do customer
    let cId: string | null = null;
    const cRes = await api.post("/admin/customers", {
      headers,
      data: { name: cliente.nome, phone: cliente.telefone, latitude: endereco.lat, longitude: endereco.lng },
    });
    if (cRes.ok()) {
      const body = await cRes.json();
      cId = body.id ?? body.customerId ?? null;
    } else if (cRes.status() === 409) {
      // Cliente já existe — busca por phone e atualiza lat/lng via PUT
      const searchRes = await api.get(
        `/admin/customers?phone=${encodeURIComponent(cliente.telefone)}&pageSize=5`,
        { headers }
      );
      if (searchRes.ok()) {
        const sb = await searchRes.json();
        const found = (sb.items ?? []).find((c: Record<string, string>) =>
          c.phone?.replace(/\D/g, "") === cliente.telefone.replace(/\D/g, "")
        );
        if (found) {
          cId = found.id;
          await api.put(`/admin/customers/${cId}`, {
            headers,
            data: { name: found.name, phone: found.phone, latitude: endereco.lat, longitude: endereco.lng },
          }).catch(() => null);
          console.log(`[setup] Cliente existente ${cId} atualizado com lat/lng`);
        }
      }
    } else {
      console.warn(`[setup] Cliente ${cliente.nome} falhou: ${cRes.status()} ${await cRes.text()}`);
    }
    if (cId) customerIds.push(cId);

    // Criar pedido de ENTREGA com customerId — o backend copia lat/lng do customer
    const lastCId = customerIds[customerIds.length - 1];
    const addressStr = `${endereco.street}, ${endereco.number} - ${endereco.neighborhood}, ${endereco.city}/${endereco.state}, CEP ${endereco.cep}`;
    const oRes = await api.post("/admin/orders/phone", {
      headers,
      data: {
        customerName:  cliente.nome,
        customerPhone: cliente.telefone,
        customerId:    lastCId ?? undefined,
        items:         [{ productId, qty: 1 }],
        paymentMethod: "PIX",
        deliveryType:  "ENTREGA",
        address:       addressStr,
      },
    });

    if (!oRes.ok()) {
      console.warn(`[setup] Pedido ${cliente.nome} falhou: ${oRes.status()} ${await oRes.text()}`);
      continue;
    }

    const oBody = await oRes.json();
    const oId = oBody.id ?? null;
    if (!oId) continue;
    orderIds.push(oId);

    // Avança status: RECEBIDO → EM_PREPARO → PRONTO_PARA_ENTREGA (delivery orders)
    await api.patch(`/orders/${oId}/status`, { headers, data: { status: "EM_PREPARO" } });
    // Tenta PRONTO_PARA_ENTREGA (pedidos de entrega) e depois PRONTO_PARA_SERVIR (fallback)
    let r2 = await api.patch(`/orders/${oId}/status`, { headers, data: { status: "PRONTO_PARA_ENTREGA" } });
    if (!r2.ok()) {
      r2 = await api.patch(`/orders/${oId}/status`, { headers, data: { status: "PRONTO_PARA_SERVIR" } });
    }
    if (!r2.ok()) {
      console.warn(`[setup] Avanço de status falhou para ${oId}: ${r2.status()} ${await r2.text()}`);
    } else {
      console.log(`[setup] Pedido ${oId} pronto para entrega.`);
    }
  }

  console.log(`[setup] ${orderIds.length} pedidos de entrega criados: ${orderIds.join(", ")}`);
  await api.dispose();
});

// ── Teardown ───────────────────────────────────────────────────────────────────

test.afterAll(async () => {
  const { api, token } = await autenticarAPI();
  if (!token) { await api.dispose(); return; }
  const headers = { Authorization: `Bearer ${token}` };

  if (routeId) {
    await api.delete(`/routes/${routeId}`, { headers }).catch(() => null);
    console.log(`[teardown] Rota ${routeId} removida.`);
  }
  for (const oId of orderIds) {
    await api.patch(`/orders/${oId}/status`, { headers, data: { status: "CANCELADO" } }).catch(() => null);
  }
  for (const cId of customerIds) {
    await api.delete(`/admin/customers/${cId}`, { headers }).catch(() => null);
  }
  console.log(`[teardown] ${orderIds.length} pedidos cancelados, ${customerIds.length} clientes removidos.`);
  await api.dispose();
});

// ── Teste ──────────────────────────────────────────────────────────────────────

test.describe("Módulo: Rotas", () => {
  test("Fluxo completo de rotas", async ({ page }) => {

    test.setTimeout(600_000); // 10 min — margem para slowMo 600ms + rede + pauses

    await configurarTema(page);

    // ════════════════════════════════════════════════════════════════════════
    // PARTE 1 — ADMIN: Criar rota
    // ════════════════════════════════════════════════════════════════════════

    // ── Cena 1: Lista de rotas ────────────────────────────────────────────
    await navegarPara(page, "/app/logistica/rotas");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(2_500);
    await ocultarLegenda(page);
    await pausaVisual(600);

    // ── Cena 2: Abre o planejador ─────────────────────────────────────────
    await page.getByRole("button", { name: /Nova rota/i }).first().click();
    await aguardarCarregamento(page);
    // A página do planner usa "Criar Rota" como heading (não h1)
    await page.waitForSelector("text=Criar Rota", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.planner);
    await pausaVisual(1_500);
    await ocultarLegenda(page);

    // Seleciona o entregador (primeiro da lista)
    const selectEntregador = page.locator("select").first();
    await selectEntregador.click();
    await pausaVisual(500);
    // Pega options disponíveis e seleciona a primeira real (não placeholder)
    const optionValues = await selectEntregador.locator("option").evaluateAll(
      (opts) => (opts as Array<{ value: string }>)
        .map((o) => o.value)
        .filter((v) => v && v !== "")
    );
    if (optionValues.length > 0) {
      await selectEntregador.selectOption(optionValues[0]);
    }
    await aguardarCarregamento(page);
    await pausaVisual(800);

    // ── Cena 3: Selecionar pedidos ────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.selecionar);
    await pausaVisual(1_000);
    await ocultarLegenda(page);

    // Aguarda a lista de pedidos aparecer (o planner usa React Query)
    await page.waitForTimeout(3_000);
    await aguardarCarregamento(page);

    // O planner renderiza <button> clicáveis para cada pedido.
    // Clica em cada botão-card pelo nome do cliente.
    let selecionados = 0;
    for (const cliente of CLIENTES_TESTE) {
      const card = page.locator("button").filter({ hasText: cliente.nome }).first();
      const visivel = await card.isVisible({ timeout: 4_000 }).catch(() => false);
      if (visivel) {
        await card.scrollIntoViewIfNeeded();
        await card.click();
        selecionados++;
        await pausaVisual(500);
      } else {
        console.warn(`[test] Card não encontrado para: ${cliente.nome}`);
      }
    }
    if (selecionados === 0) {
      console.warn("[test] Nenhum card de pedido clicável encontrado no planner.");
      test.skip(true, "Planner sem pedidos selecionáveis — verifique setup.");
      return;
    }
    await pausaVisual(800);

    // ── Cena 4: Preview / Criação da rota ────────────────────────────────
    // Tenta "Visualizar rota bidirecional" (para pedidos com GPS).
    // Se não disponível, clica em "Criar rota" diretamente (pedidos sem GPS).
    const btnPreview = page.getByRole("button", { name: /Visualizar rota bidirecional/i });
    const previewVisivel = await btnPreview.isVisible({ timeout: 3_000 }).catch(() => false);
    if (previewVisivel) {
      await btnPreview.click();
      await aguardarCarregamento(page);
      await mostrarLegenda(page, LEGENDAS.preview);
      await pausaVisual(2_500);
      await ocultarLegenda(page);
      // Seleciona Rota A se disponível
      const rotaACard = page.getByText("Rota A").first();
      if (await rotaACard.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await rotaACard.click();
        await pausaVisual(600);
      }
    }

    // ── Cena 5: Criar rota ────────────────────────────────────────────────
    const btnCriar = page.getByRole("button", { name: /Criar rota/i });
    await btnCriar.waitFor({ state: "visible", timeout: 10_000 });
    await btnCriar.click();
    await aguardarCarregamento(page);

    // Captura o routeId da URL após redirecionamento
    await page.waitForURL(/\/app\/logistica\/rotas\/(?!planner)[a-z0-9-]+/, { timeout: 20_000 }).catch(() => null);
    const routeURL = page.url();
    const uuidMatch = routeURL.match(/\/rotas\/([a-f0-9-]{36})/);
    if (uuidMatch) {
      routeId = uuidMatch[1];
    } else {
      // Rota pode estar na lista — busca o ID via API
      const { api: listApi, token: listToken } = await autenticarAPI();
      if (listToken) {
        const lr = await listApi.get("/routes?pageSize=1&orderBy=createdAt&desc=true", {
          headers: { Authorization: `Bearer ${listToken}` },
        });
        if (lr.ok()) {
          const lb = await lr.json();
          routeId = lb.items?.[0]?.id ?? lb[0]?.id ?? null;
        }
      }
      await listApi.dispose();
      if (routeId) {
        await navegarPara(page, `/app/logistica/rotas/${routeId}`);
        await aguardarCarregamento(page);
      }
    }

    // ── Cena 6: Detalhe da rota (admin) ──────────────────────────────────
    await injetarUITutorial(page); // pode ter sido full reload após criação
    // A página de detalhe usa div/span — não necessariamente h1
    await page.waitForSelector("text=Paradas", { state: "visible", timeout: 20_000 }).catch(() => null);

    await mostrarLegenda(page, LEGENDAS.detalhe);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
    await pausaVisual(600);

    // ════════════════════════════════════════════════════════════════════════
    // PARTE 2 — PAINEL DO MOTOBOY
    // ════════════════════════════════════════════════════════════════════════

    // ── Cena 7: Login do motoboy ──────────────────────────────────────────
    await navegarPara(page, "/deliverer/login");
    await injetarUITutorial(page);
    await page.waitForSelector("input#phone", { state: "visible", timeout: 15_000 });

    // Preenche telefone (campo com máscara — digita char a char)
    const campoTelefone = page.locator("input#phone");
    await campoTelefone.click();
    for (const char of CREDENCIAIS.usuarioMotoboy) {
      await campoTelefone.type(char, { delay: 70 });
    }
    await pausaVisual(600);

    // Preenche PIN (4 inputs separados)
    const pinInputs = page.locator('input[type="password"]');
    const pin = CREDENCIAIS.senhaMotoboy;
    for (let i = 0; i < pin.length; i++) {
      await pinInputs.nth(i).click();
      await pinInputs.nth(i).fill(pin[i]);
      await pausaVisual(150);
    }
    await pausaVisual(600);

    await page.getByRole("button", { name: /Entrar/i }).click();
    await aguardarCarregamento(page);
    await page.waitForURL(/\/deliverer/, { timeout: 15_000 }).catch(() => null);

    // ── Cena 8: Home do motoboy ───────────────────────────────────────────
    // O painel do motoboy não usa h1/h2 — aguarda qualquer conteúdo visível
    await page.waitForLoadState("domcontentloaded");
    await pausaVisual(1_000);

    await mostrarLegenda(page, LEGENDAS.motoBoyHome);
    await pausaVisual(2_500);
    await ocultarLegenda(page);
    await pausaVisual(600);

    // Clica em "Ver Rota"
    const btnVerRota = page.getByRole("button", { name: /Ver Rota/i }).first();
    await btnVerRota.waitFor({ state: "visible", timeout: 15_000 });
    await btnVerRota.click();
    await aguardarCarregamento(page);

    // ── Cena 9: Iniciar rota (motoboy) ────────────────────────────────────
    // Aguarda o botão "Iniciar Rota" ou "PRÓXIMA PARADA" aparecer na rota detail
    await page.waitForSelector("text=Iniciar Rota, text=PRÓXIMA PARADA", { state: "visible", timeout: 15_000 }).catch(() => null);

    await mostrarLegenda(page, LEGENDAS.iniciarRota);
    await pausaVisual(1_500);
    await ocultarLegenda(page);

    const btnIniciar = page.getByRole("button", { name: /Iniciar Rota/i });
    if (await btnIniciar.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await btnIniciar.click();
      await aguardarCarregamento(page);
      await pausaVisual(1_000);
    }

    // ── Cena 10: 1ª Parada — Entregue ✓ ──────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.parada1);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    // Mostra os botões de contato brevemente
    const btnWhatsApp = page.getByRole("button", { name: /WhatsApp/i }).first();
    if (await btnWhatsApp.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await btnWhatsApp.scrollIntoViewIfNeeded();
      await pausaVisual(800);
    }

    // Clica Entregue
    const btnEntregue = page.getByRole("button", { name: /Entregue/i }).first();
    await btnEntregue.waitFor({ state: "visible", timeout: 15_000 });
    await btnEntregue.click();
    await aguardarCarregamento(page);
    await pausaVisual(600);

    // Overlay WhatsApp — entrega confirmada
    await mostrarOverlayWhatsApp(page, "entregue", CLIENTES_TESTE[0].nome, "pedido");

    // ── Cena 11: 2ª Parada — Pular ───────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.parada2);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    const btnPular = page.getByRole("button", { name: /^Pular$/i }).first();
    await btnPular.waitFor({ state: "visible", timeout: 15_000 });
    await btnPular.click();
    await pausaVisual(500);

    // Modal de motivo — seleciona "Passarei depois"
    const motivoPular = page.getByText("Passarei depois");
    await motivoPular.waitFor({ state: "visible", timeout: 8_000 });
    await motivoPular.click();
    await pausaVisual(800);

    const btnConfirmarPular = page.getByRole("button", { name: /Confirmar Pular/i });
    await btnConfirmarPular.click();
    await aguardarCarregamento(page);
    await pausaVisual(800);

    // ── Cena 12: 3ª Parada — Falhou ✗ ────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.parada3);
    await pausaVisual(2_000);
    await ocultarLegenda(page);

    const btnFalhou = page.getByRole("button", { name: /Falhou/i }).first();
    await btnFalhou.waitFor({ state: "visible", timeout: 15_000 });
    await btnFalhou.click();
    await pausaVisual(500);

    // Modal de motivo — seleciona "Cliente ausente"
    const motivoFalha = page.getByText("Cliente ausente");
    await motivoFalha.waitFor({ state: "visible", timeout: 8_000 });
    await motivoFalha.click();
    await pausaVisual(800);

    const btnConfirmarFalha = page.getByRole("button", { name: /Confirmar Falha/i });
    await btnConfirmarFalha.click();
    await aguardarCarregamento(page);
    await pausaVisual(600);

    // Overlay WhatsApp — notificação de falha
    await mostrarOverlayWhatsApp(page, "falha", CLIENTES_TESTE[2].nome, "pedido");

    // ── Cena 13: Rota concluída (motoboy) ─────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.concluida);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
    await pausaVisual(600);

    // ════════════════════════════════════════════════════════════════════════
    // PARTE 3 — ADMIN: Resultado final
    // ════════════════════════════════════════════════════════════════════════

    // ── Cena 14: Detalhe da rota (admin) — resultado ──────────────────────
    const adminRoutePath = routeId
      ? `/app/logistica/rotas/${routeId}`
      : "/app/logistica/rotas";
    await navegarPara(page, adminRoutePath);
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });
    await aguardarCarregamento(page);

    await mostrarLegenda(page, LEGENDAS.adminFinal);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
    await pausaVisual(800);

    // Scroll lento para mostrar a lista de paradas com os status
    await page.mouse.wheel(0, 350);
    await pausaVisual(1_200);
    await page.mouse.wheel(0, 350);
    await pausaVisual(2_000);

    // ── Cena 15: Lista de rotas — visão final ─────────────────────────────
    await navegarPara(page, "/app/logistica/rotas");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });
    await pausaVisual(2_500);
  });
});
