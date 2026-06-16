# Roteiro: Visualizar detalhes do pedido

**Vídeo:** `06-visualizar-detalhes`  
**Módulo:** Pedidos  
**Duração estimada:** ~50 segundos

---

## Objetivo

Apresentar todas as seções da página de detalhe de um pedido.

---

## Cenas

### Cena 1 — Cabeçalho
- Tela: `/app/pedidos/:orderNumber`
- Legenda: *"O cabeçalho do pedido exibe o número, status atual e botões de ação."*
- Pausa: 2,5 s

### Cena 2 — Painel de status
- Legenda: *"O painel de status mostra o fluxo completo do atendimento. O botão iluminado é o status atual."*
- Pausa: 2,5 s

### Cena 3 — Itens do pedido
- Ação: scroll 300 px
- Legenda: *"Os itens do pedido mostram produto, quantidade e valor unitário. O total é calculado automaticamente."*
- Pausa: 2,5 s

### Cena 4 — Pagamento e cliente
- Ação: scroll 300 px
- Legenda: *"Forma de pagamento e dados do cliente ficam registrados para consulta e comprovante."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa       | Ação                                                              |
|-------------|-------------------------------------------------------------------|
| `beforeAll` | `GET /admin/products?active=true&pageSize=1` → obtém productId   |
| `beforeAll` | `POST /admin/orders/phone` → cria pedido "Igor Ferreira" em RECEBIDO |
| `afterAll`  | `PATCH /orders/:id/status` → cancela o pedido                    |
