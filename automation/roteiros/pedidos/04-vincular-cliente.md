# Roteiro: Vincular cliente ao pedido

**Vídeo:** `04-vincular-cliente`  
**Módulo:** Pedidos  
**Duração estimada:** ~45 segundos

---

## Objetivo

Demonstrar como buscar um cliente cadastrado e vinculá-lo a um novo pedido.

---

## Cenas

### Cena 1 — Digitar telefone
- Tela: `/app/atendimento/pedido`
- Legenda: *"Digite o telefone ou CPF do cliente no campo de busca. O sistema localiza o cadastro em tempo real."*
- Ação: digita o telefone do cliente (delay 60 ms/char) → clica "Buscar"

### Cena 2 — Cliente encontrado
- Legenda: *"Cliente encontrado! O sistema exibe o nome, telefone e endereço do cadastro."*
- Aguarda "Cliente encontrado" aparecer
- Pausa: 2,5 s

### Cena 3 — Confirmar
- Legenda: *"Clique em Confirmar e montar pedido para vincular este cliente ao novo atendimento."*
- Ação: clica "Confirmar e montar pedido →"

### Cena 4 — Carrinho aberto
- Aguarda campo de busca de produtos
- Legenda: *"Cliente vinculado! O carrinho de produtos é aberto com os dados do cliente já preenchidos."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa       | Ação                                                              |
|-------------|-------------------------------------------------------------------|
| `beforeAll` | `POST /admin/customers` → cria "Gabriela Rocha" com telefone único |
| `afterAll`  | `DELETE /admin/customers/:id` → remove o cliente de demo          |
