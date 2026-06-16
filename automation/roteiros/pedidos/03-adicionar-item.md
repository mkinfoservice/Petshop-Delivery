# Roteiro: Adicionar item ao pedido

**Vídeo:** `03-adicionar-item`  
**Módulo:** Pedidos  
**Duração estimada:** ~45 segundos

---

## Objetivo

Demonstrar como buscar e adicionar produtos ao carrinho no PhoneOrderBuilder.

---

## Cenas

### Cena setup (não narrada)
- Usa modo "Sem cadastro": digita "Visitante Teste" → "Ir para produtos →"
- Chega diretamente à etapa de carrinho sem beforeAll

### Cena 1 — Busca de produto
- Legenda: *"Digite o nome do produto no campo de busca para filtrar o catálogo em tempo real."*
- Ação: digita "Peti" devagar → pausa → limpa campo

### Cena 2 — Adicionar produto
- Legenda: *"Clique no produto para adicioná-lo ao carrinho. Se houver adicionais, uma tela de configuração é exibida."*
- Ação: clica no primeiro produto → confirma modal "Adicionar" se abrir

### Cena 3 — Carrinho atualizado
- Legenda: *"O carrinho mostra os itens selecionados com quantidade e valor. Ajuste a quantidade com os botões + e −."*
- Pausa: 2,5 s

### Cena 4 — Próximo passo
- Legenda: *"Com o carrinho montado, clique em Ir para pagamento para prosseguir com o atendimento."*
- Pausa: 2 s

---

## Observações

- Não cria pedido real — o vídeo termina antes do pagamento.
- Não precisa de beforeAll/afterAll.
