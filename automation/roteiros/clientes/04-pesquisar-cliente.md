# Roteiro — 04: Pesquisar cliente

**Módulo:** Clientes  
**Arquivo de teste:** `automation/playwright/tests/clientes/04-pesquisar-cliente.spec.ts`  
**Duração estimada do vídeo:** 45 s a 1 minuto  
**Nível:** Iniciante

---

## Objetivo do vídeo

Demonstrar como usar a busca na lista de clientes para localizar rapidamente
um registro pelo nome ou telefone.

---

## Caminho de navegação

```
/app/atendimento/clientes → (busca) → resultado filtrado → ficha do cliente
```

---

## Roteiro narrado (cena a cena)

### Cena 1 — Lista de Clientes (0:00 – 0:15)
**Legenda:** "Na lista de clientes, use o campo de busca para localizar um registro."

### Cena 2 — Digitando o nome (0:15 – 0:35)
**Legenda:** "Digite o nome ou telefone do cliente.\nO sistema filtra os resultados em tempo real."

### Cena 3 — Resultado e acesso (0:35 – 1:00)
**Legenda:** "Clique no cliente encontrado para abrir a ficha completa."

---

## Checklist
- [ ] Campo de busca é visível e clicável
- [ ] Resultado aparece após digitação
- [ ] Clique no resultado navega para a ficha
