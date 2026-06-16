# Roteiro — 02: Dashboard — Visão geral

**Módulo:** Dashboard  
**Arquivo de teste:** `automation/playwright/tests/dashboard/02-visao-geral.spec.ts`  
**Duração estimada do vídeo:** 1 a 1,5 minutos  
**Nível:** Iniciante

---

## Objetivo do vídeo

Apresentar o painel principal do vendApps: menu lateral, indicadores,
atalhos rápidos e como navegar entre os módulos do sistema.

---

## Pré-requisitos

- Usuário autenticado no sistema

---

## Caminho de navegação

```
/app (painel principal)
  ├── Menu lateral → módulos do sistema
  └── /app/atendimento → hub de atendimento
```

---

## Roteiro narrado (cena a cena)

### Cena 1 — Painel principal (0:00 – 0:20)

**O que aparece na tela:**
Dashboard com menu lateral visível e indicadores de resumo.

**Legenda exibida no vídeo:**
> "Este é o painel principal do vendApps.\nAqui você tem acesso a todos os módulos do sistema."

**Narração sugerida:**
> "Ao fazer login, você chega ao painel principal.
> No menu lateral estão todos os módulos disponíveis para sua empresa."

---

### Cena 2 — Menu lateral (0:20 – 0:40)

**O que aparece na tela:**
Cursor percorre os itens do menu lateral.

**Legenda exibida no vídeo:**
> "O menu lateral reúne os módulos:\nAtendimento, Pedidos, Catálogo, Financeiro e mais."

**Narração sugerida:**
> "O menu lateral organiza os módulos do sistema.
> Cada seção agrupa as funcionalidades relacionadas."

---

### Cena 3 — Hub de Atendimento (0:40 – 1:10)

**O que aparece na tela:**
Clique em Atendimento, página `/app/atendimento` carrega com atalhos visíveis.

**Legenda exibida no vídeo:**
> "O módulo de Atendimento reúne os atalhos mais usados do dia a dia."

**Narração sugerida:**
> "O módulo de Atendimento é o ponto de partida para criar pedidos,
> cadastrar clientes e consultar o histórico de atendimentos."

---

### Cena 4 — Retorno ao dashboard (1:10 – 1:30)

**O que aparece na tela:**
Clique no logo ou ícone home, retorno ao `/app`.

**Legenda exibida no vídeo:**
> "Clique no logo ou no ícone inicial para voltar ao painel principal a qualquer momento."

---

## Resultado esperado

- Viewer entende a estrutura de navegação do sistema
- Cenas mostram dashboard → atendimento → retorno

---

## Checklist de validação antes de publicar

- [ ] Dashboard carrega completamente antes do vídeo iniciar
- [ ] Menu lateral está visível
- [ ] Navegação até Atendimento é mostrada com cursor visível
- [ ] Retorno ao dashboard é demonstrado
- [ ] Duração entre 1 min e 1 min 30 s

---

## Arquivos relacionados

| Item | Caminho |
|---|---|
| Teste E2E | `automation/playwright/tests/dashboard/02-visao-geral.spec.ts` |
| Vídeo gerado | `E:\videos-vendapps\...\video.webm` |
