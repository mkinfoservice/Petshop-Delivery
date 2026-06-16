# Roteiro: Cadastrar produto

**Vídeo:** `02-cadastrar-produto`
**Módulo:** Produtos
**Duração estimada:** ~60 segundos

---

## Objetivo

Demonstrar o fluxo completo de cadastro de um novo produto.

---

## Dados utilizados

| Campo     | Valor                      |
|-----------|----------------------------|
| Nome      | Ração Premium para Cães    |
| Categoria | Primeira disponível        |
| Preço     | R$ 25,90                   |

---

## Cenas

### Cena 1 — Formulário de cadastro
- Tela: `/app/produtos/new`
- Legenda: *"Preencha os dados do produto. Campos marcados com * são obrigatórios."*
- Pausa: 2 s

### Cena 2 — Nome do produto
- Legenda: *"Digite o nome do produto. O sistema gera o slug (URL) automaticamente."*
- Ação: digita "Ração Premium para Cães" no campo Nome (delay 55 ms/char)
- Pausa: 1,2 s

### Cena 3 — Categoria
- Legenda: *"Selecione a categoria do produto. Ela organiza o catálogo para os clientes."*
- Ação: aguarda opções carregarem → seleciona primeiro item do select `[required]`
- Pausa: 1 s

### Cena 4 — Preço
- Legenda: *"Informe o preço de venda e o custo. O sistema calcula a margem em tempo real."*
- Ação: clica em Preço de venda, seleciona tudo, digita "25,90"
- Pausa: 1,5 s

### Cena 5 — Salvar
- Legenda: *"Clique em Criar produto para finalizar o cadastro."*
- Ação: clica no botão "Criar produto"
- Aguarda redirect para `/app/produtos/:id`

### Cena 6 — Resultado
- Tela: ficha de edição do produto recém-criado
- Legenda: *"Produto cadastrado com sucesso! Você é redirecionado para a ficha de edição."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa      | Ação                                                         |
|------------|--------------------------------------------------------------|
| `afterAll` | `DELETE /admin/products/{id}` — remove o produto de demo     |

O ID é capturado a partir da URL após o redirect pós-criação.

---

## Observações

- A Categoria é obrigatória (`required`). O teste aguarda `options.length > 1` antes de selecionar.
- O teardown usa o `produtoIdCriado` capturado no corpo do teste.
