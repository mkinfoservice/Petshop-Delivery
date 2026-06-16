# Roteiro: Listar produtos

**Vídeo:** `03-listar-produtos`
**Módulo:** Produtos
**Duração estimada:** ~40 segundos

---

## Objetivo

Apresentar a listagem de produtos, filtros disponíveis e rolagem da lista.

---

## Cenas

### Cena 1 — Lista de Produtos
- Tela: `/app/produtos`
- Legenda: *"A lista de produtos exibe todo o catálogo cadastrado. Cada linha mostra nome, categoria, preço e margem."*
- Pausa: 3 s

### Cena 2 — Filtros e busca
- Legenda: *"Use os filtros de status e a barra de busca para localizar produtos rapidamente."*
- Ação: digita "Racao" no campo de busca (devagar), pausa 1,5 s, limpa o campo

### Cena 3 — Rolagem
- Legenda: *"Role a lista para visualizar todos os produtos. Clique em qualquer linha para abrir o cadastro."*
- Ação: `mouse.wheel(0, 350)` → pausa 1,5 s → `mouse.wheel(0, -350)` → pausa 0,6 s

---

## Observações

- Não clica em nenhum produto — a navegação para ficha é coberta pelo `04-editar-produto`.
- A busca digita "Racao" (sem cedilha) para evitar problemas de encoding no `type()`.
