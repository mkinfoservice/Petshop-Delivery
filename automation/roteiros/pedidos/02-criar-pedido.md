# Roteiro: Criar pedido

**Vídeo:** `02-criar-pedido`  
**Módulo:** Pedidos  
**Duração estimada:** ~80 segundos

---

## Objetivo

Demonstrar o fluxo completo de criação de pedido pelo PhoneOrderBuilder (5 etapas).

---

## Cenas

### Cena 1 — Identificar cliente
- Tela: `/app/atendimento/pedido`
- Ação: digita o telefone do cliente no campo "Telefone ou CPF" → clica "Buscar"
- Aguarda "Cliente encontrado"
- Legenda: *"Digite o telefone ou CPF do cliente para localizá-lo no sistema."*

### Cena 2 — Confirmar cliente
- Legenda: *"Cliente encontrado! Confirme para montar o pedido."*
- Ação: clica "Confirmar e montar pedido →"

### Cena 3 — Montar carrinho
- Legenda: *"Selecione os produtos do catálogo. Clique para adicionar ao carrinho."*
- Ação: clica no primeiro produto do grid → confirma modal de adicionais se abrir
- Clica "Ir para pagamento →"

### Cena 4 — Pagamento
- Legenda: *"Escolha a forma de pagamento e revise o total do pedido."*
- PIX é o padrão — pausa 2 s → clica "Ver resumo →"

### Cena 5 — Resumo
- Legenda: *"Confirme os dados do pedido antes de finalizar."*
- Pausa: 2 s → clica "Confirmar pedido"

### Cena 6 — Pedido confirmado
- Tela: tela de sucesso com número do pedido
- Legenda: *"Pedido confirmado! O número fica registrado para acompanhamento."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa      | Ação                                                             |
|------------|------------------------------------------------------------------|
| `beforeAll`| `POST /admin/customers` → cria "Fernando Gomes" com telefone único |
| `afterAll` | `PATCH /orders/:id/status` → cancela o pedido de demo            |
| `afterAll` | `DELETE /admin/customers/:id` → remove o cliente de demo         |
