# Cronograma de Gravações — vendApps Tutoriais

**Sistema:** https://novaempresa.vendapps.com.br  
**Vídeos gerados em:** `E:\videos-vendapps\`  
**Código-fonte:** `D:\dev\vendapps\automation\playwright\`

---

## Convenções obrigatórias

### Organização dos vídeos
Cada vídeo é salvo em sua própria pasta nomeada pelo slug do fluxo:
```
E:\videos-vendapps\{slug}\{slug}.webm
```
Exemplo: `E:\videos-vendapps\criar_pedido\criar_pedido.webm`

Se o arquivo já existir, o `video-organizer.ts` arquiva o anterior com sufixo de data/hora —
**nunca sobrescreve**. O acervo é acumulativo.

> Manter `["./video-organizer.ts"]` nos reporters do `playwright.config.ts`. Nunca apontar
> `outputDir` diretamente para `E:/videos-vendapps/`.

### Elementos visuais do tutorial
| Elemento | Status | Motivo |
|---|---|---|
| Cursor roxo (`#pw-cursor`) | ✅ Manter | Mostra posição do mouse claramente |
| Legendas (`#pw-caption`) | ✅ Manter | Contexto para o espectador |
| Ripple de clique | ❌ Removido | Dessincronizado com slowMo 600ms |
| Seta ▼ sobre elemento | ❌ Removido | Aparece no momento errado — parece lag |
| Contorno roxo no elemento | ❌ Removido | Mesmo problema de dessincronização |

---

## Como executar uma gravação

```bash
cd D:/dev/vendapps/automation/playwright

# Gravar um fluxo específico
npx playwright test tests/{modulo}/NN-nome.spec.ts --project=tutoriais

# Gravar todos os fluxos de um módulo
npx playwright test tests/clientes/ --project=tutoriais

# Gravar tudo
npx playwright test --project=tutoriais
```

---

## FASE 1 — Preparação da Base ✅ CONCLUÍDA

| Item | Status |
|---|---|
| Playwright configurado | ✅ |
| Auth globalSetup (login + storageState) | ✅ |
| helpers/navigation.ts | ✅ |
| helpers/form.ts | ✅ |
| helpers/tutorial.ts (cursor, ripple, seta, legendas) | ✅ |
| Documentação de estrutura | ✅ |

---

## FASE 2 — Fluxos Críticos de Entrada

| # | Fluxo | Roteiro | Teste | Vídeo | Status |
|---|---|---|---|---|---|
| 01 | Login no sistema | roteiros/dashboard/01-login.md | tests/dashboard/01-login.spec.ts | ✅ | ✅ Concluído |
| 02 | Dashboard — visão geral | roteiros/dashboard/02-visao-geral.md | tests/dashboard/02-visao-geral.spec.ts | ✅ | ✅ Concluído |
| 03 | Acessar módulo Clientes | roteiros/clientes/02-acessar-clientes.md | tests/clientes/02-acessar-clientes.spec.ts | ✅ | ✅ Concluído |
| 04 | Cadastrar cliente | roteiros/clientes/01-cadastrar-cliente.md | tests/clientes/01-cadastrar-cliente.spec.ts | ✅ | ✅ Concluído |
| 05 | Listar clientes | roteiros/clientes/03-listar-clientes.md | tests/clientes/03-listar-clientes.spec.ts | ✅ | ✅ Concluído |
| 06 | Pesquisar cliente | roteiros/clientes/04-pesquisar-cliente.md | tests/clientes/04-pesquisar-cliente.spec.ts | ✅ | ✅ Concluído |
| 07 | Editar cliente | roteiros/clientes/05-editar-cliente.md | tests/clientes/05-editar-cliente.spec.ts | ✅ | ✅ Concluído |

---

## FASE 3 — Catálogo

| # | Fluxo | Roteiro | Teste | Vídeo | Status |
|---|---|---|---|---|---|
| 01 | Acessar produtos | roteiros/produtos/01-acessar-produtos.md | tests/produtos/01-acessar-produtos.spec.ts | ✅ | ✅ Concluído |
| 02 | Cadastrar produto | roteiros/produtos/02-cadastrar-produto.md | tests/produtos/02-cadastrar-produto.spec.ts | ✅ | ✅ Concluído |
| 03 | Listar produtos | roteiros/produtos/03-listar-produtos.md | tests/produtos/03-listar-produtos.spec.ts | ✅ | ✅ Concluído |
| 04 | Editar produto | roteiros/produtos/04-editar-produto.md | tests/produtos/04-editar-produto.spec.ts | ✅ | ✅ Concluído |

---

## FASE 4 — Pedidos

| # | Fluxo | Roteiro | Teste | Vídeo | Status |
|---|---|---|---|---|---|
| 01 | Acessar pedidos | roteiros/pedidos/01-acessar-pedidos.md | tests/pedidos/01-acessar-pedidos.spec.ts | ✅ | ✅ Concluído |
| 02 | Criar pedido | roteiros/pedidos/02-criar-pedido.md | tests/pedidos/02-criar-pedido.spec.ts | ✅ | ✅ Concluído |
| 03 | Adicionar item ao pedido | roteiros/pedidos/03-adicionar-item.md | tests/pedidos/03-adicionar-item.spec.ts | ✅ | ✅ Concluído |
| 04 | Vincular cliente ao pedido | roteiros/pedidos/04-vincular-cliente.md | tests/pedidos/04-vincular-cliente.spec.ts | ✅ | ✅ Concluído |
| 05 | Alterar status do pedido | roteiros/pedidos/05-alterar-status.md | tests/pedidos/05-alterar-status.spec.ts | ✅ | ✅ Concluído |
| 06 | Visualizar detalhes do pedido | roteiros/pedidos/06-visualizar-detalhes.md | tests/pedidos/06-visualizar-detalhes.spec.ts | ✅ | ✅ Concluído |
| 07 | Finalizar pedido | roteiros/pedidos/07-finalizar-pedido.md | tests/pedidos/07-finalizar-pedido.spec.ts | ✅ | ✅ Concluído |

---

## FASE 5 — Dashboard

| # | Fluxo | Roteiro | Teste | Status |
|---|---|---|---|---|
| 03 | Visualizar indicadores | roteiros/dashboard/03-indicadores.md | tests/dashboard/03-indicadores.spec.ts | ✅ Concluído |
| 04 | Navegar pelos atalhos | roteiros/dashboard/04-atalhos.md | tests/dashboard/04-atalhos.spec.ts | ✅ Concluído |

---

## FASE 6 — Administrativo

| # | Fluxo | Roteiro | Teste | Vídeo | Status |
|---|---|---|---|---|---|
| 01 | Equipe (usuários) | roteiros/admin/01-equipe.md | tests/admin/01-equipe.spec.ts | ✅ | ✅ Concluído |
| 02 | Financeiro | roteiros/admin/02-financeiro.md | tests/admin/02-financeiro.spec.ts | ✅ | ✅ Concluído |
| 03 | Rotas | roteiros/admin/03-rotas.md | tests/admin/03-rotas.spec.ts | ✅ | ✅ Concluído |
| 04 | Entregadores | roteiros/admin/04-entregadores.md | tests/admin/04-entregadores.spec.ts | ✅ | ✅ Concluído |
| 05 | Relatórios | roteiros/admin/05-relatorios.md | tests/admin/05-relatorios.spec.ts | ✅ | ✅ Concluído |

---

## FASE 7 — Fluxo Completo de Rotas ✅ CONCLUÍDA

| # | Fluxo | Roteiro | Teste | Vídeo | Status |
|---|---|---|---|---|---|
| 01 | Fluxo completo de rotas (~3 min) | roteiros/rotas/completo-rotas.md | tests/rotas/completo-rotas.spec.ts | ✅ | ✅ Concluído |

Cobre o ciclo completo: Admin cria rota com 3 paradas → Motoboy executa (Entregue / Pular / Falhou) → Notificações WhatsApp simuladas → Admin vê resultado final.

---

## FASE 8 — Revisão Final ✅ CONCLUÍDA

- Revisar nomes de arquivos e vídeos
- Revisar consistência dos roteiros
- Consolidar documentação
- Padronizar checklist de publicação
