# Roteiro: Finalizar pedido

**Vídeo:** `07-finalizar-pedido`  
**Módulo:** Pedidos  
**Duração estimada:** ~50 segundos

---

## Objetivo

Demonstrar como avançar um pedido até o status ENTREGUE.

---

## Cenas

### Cena 1 — Fluxo de status (em EM_PREPARO)
- Tela: `/app/pedidos/:orderNumber` (criado em EM_PREPARO pelo beforeAll)
- Legenda: *"O fluxo de atendimento mostra cada etapa até a entrega. Clique nos botões para avançar o status."*
- Pausa: 2,5 s

### Cena 2 — Pronto para servir
- Legenda: *"Clique em Pronto para servir quando o pedido estiver preparado."*
- Ação: clica "Pronto para servir" → aguarda atualização → pausa 1,2 s

### Cena 3 — Entregue
- Legenda: *"Clique em Entregue para registrar que o atendimento foi concluído."*
- Ação: clica "Entregue" → aguarda atualização → pausa 1,2 s

### Cena 4 — Finalizado
- Legenda: *"Pedido finalizado como Entregue! O atendimento fica registrado no histórico."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa       | Ação                                                                      |
|-------------|---------------------------------------------------------------------------|
| `beforeAll` | `GET /admin/products?active=true&pageSize=1` → obtém productId            |
| `beforeAll` | `POST /admin/orders/phone` → cria pedido "Juliana Alves" em RECEBIDO      |
| `beforeAll` | `PATCH /orders/:id/status` → avança para EM_PREPARO                       |
| Sem teardown| Pedido fica em ENTREGUE (estado final válido para dados históricos)        |
