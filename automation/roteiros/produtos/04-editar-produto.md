# Roteiro: Editar produto

**Vídeo:** `04-editar-produto`
**Módulo:** Produtos
**Duração estimada:** ~50 segundos

---

## Objetivo

Demonstrar como editar os dados de um produto existente.

---

## Dados utilizados

| Campo         | Original                       | Editado                              |
|---------------|--------------------------------|--------------------------------------|
| Nome          | Petisco Dental para Cães       | Petisco Dental Premium para Cães     |

---

## Cenas

### Cena 1 — Tela de edição
- Tela: `/app/produtos/:id` (ID criado no `beforeAll`)
- Legenda: *"Esta é a tela de edição do produto com todos os dados cadastrados."*
- Pausa: 2,5 s

### Cena 2 — Alterar o nome
- Legenda: *"Altere o campo desejado. Todos os dados podem ser modificados aqui."*
- Ação: clica no campo Nome, seleciona tudo, digita o novo nome (delay 55 ms/char)
- Pausa: 1,2 s

### Cena 3 — Salvar
- Legenda: *"Clique em Salvar alterações para confirmar."*
- Ação: clica no botão "Salvar alterações"
- Aguarda redirect para `/app/produtos`

### Cena 4 — Resultado
- Tela: lista de produtos
- Legenda: *"Produto atualizado com sucesso! As alterações são refletidas imediatamente no catálogo."*
- Pausa: 3 s

---

## Setup / Teardown

| Etapa       | Ação                                                                              |
|-------------|-----------------------------------------------------------------------------------|
| `beforeAll` | `GET /catalog/{slug}/categories` → obtém `categoryId`                             |
| `beforeAll` | `POST /admin/products` → cria "Petisco Dental para Cães" com o `categoryId`       |
| `afterAll`  | `DELETE /admin/products/{id}` → remove o produto de teste                         |

O slug da empresa é extraído de `CREDENCIAIS.baseURL` (primeiro segmento do hostname).

---

## Observações

- Após `updateMut.mutateAsync()`, o frontend navega para `/app/produtos` (lista).
- O teste valida `toHaveURL(/\/app\/produtos($|\?)/)`.
- Se o `beforeAll` falhar (API fria), o teste é ignorado graciosamente via `test.skip`.
