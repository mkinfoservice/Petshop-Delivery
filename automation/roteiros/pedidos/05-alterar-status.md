# Roteiro: Alterar status do pedido

**Vídeo:** `05-alterar-status`  
**Módulo:** Pedidos  
**Duração estimada:** ~40 segundos

---

## Objetivo

Demonstrar como avançar o status de um pedido no painel de detalhes.

---

## Cenas

### Cena 1 — Status atual
- Tela: `/app/pedidos/:orderNumber` (criado em RECEBIDO)
- Legenda: *"O painel de status mostra onde o pedido está no fluxo de atendimento. O botão destacado é o status atual."*
- Pausa: 2,5 s

### Cena 2 — Alterar
- Legenda: *"Clique no próximo status para avançar o pedido. Apenas o próximo passo está habilitado para garantir o fluxo correto."*
- Ação: clica no botão "Em preparo"
- Aguarda atualização

### Cena 3 — Confirmação
- Legenda: *"Status atualizado para Em preparo! A alteração fica registrada no histórico do pedido."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa       | Ação                                                             |
|-------------|------------------------------------------------------------------|
| `beforeAll` | `GET /admin/products?active=true&pageSize=1` → obtém productId  |
| `beforeAll` | `POST /admin/orders/phone` → cria pedido "Helena Costa" em RECEBIDO |
| `afterAll`  | `PATCH /orders/:id/status` → cancela o pedido                   |
