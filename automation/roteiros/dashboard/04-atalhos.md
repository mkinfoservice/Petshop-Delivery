# Roteiro — 04: Dashboard — Atalhos rápidos

**Módulo:** Dashboard  
**Arquivo de teste:** `automation/playwright/tests/dashboard/04-atalhos.spec.ts`  
**Duração estimada do vídeo:** 1 a 1,5 minutos  
**Nível:** Iniciante

---

## Objetivo do vídeo

Mostrar como usar os atalhos do painel principal: o botão "Montar Pedido"
e os cards clicáveis que filtram listas diretamente.

---

## Pré-requisitos

- Usuário autenticado no sistema

---

## Caminho de navegação

```
/app
  ├── Clique "Montar Pedido" → /app/atendimento/pedido
  ├── Retorno → /app
  └── Clique card "Recebidos" → /app/pedidos?status=RECEBIDO
```

---

## Roteiro narrado (cena a cena)

### Cena 1 — Dashboard com botão em destaque (0:00 – 0:20)

**O que aparece na tela:**
Dashboard com o botão roxo "Montar Pedido — Atendimento" visível no topo.

**Legenda exibida no vídeo:**
> "O botão 'Montar Pedido' no topo do painel\nabre diretamente o fluxo de atendimento."

---

### Cena 2 — Clique em Montar Pedido (0:20 – 0:40)

**O que aparece na tela:**
Clique no botão → navega para `/app/atendimento/pedido`.

**Legenda exibida no vídeo:**
> "Ao clicar, você acessa o PhoneOrderBuilder\npronto para buscar o cliente e montar o pedido."

---

### Cena 3 — Retorno ao dashboard (0:40 – 0:50)

**O que aparece na tela:**
Navegação de volta para `/app` pelo menu lateral.

**Legenda exibida no vídeo:**
> "Retorne ao painel pelo menu lateral a qualquer momento."

---

### Cena 4 — Cards clicáveis de pedidos (0:50 – 1:20)

**O que aparece na tela:**
Hover no card "Recebidos" → clique → lista filtrada por status.

**Legenda exibida no vídeo:**
> "Os cards de status também são atalhos:\nclicar em 'Recebidos' abre a lista de pedidos filtrada por esse status."

---

## Resultado esperado

- Viewer entende como usar o botão de atendimento rápido
- Demonstração do atalho de cards → lista filtrada

---

## Checklist de validação antes de publicar

- [ ] Botão "Montar Pedido" visível e clicável
- [ ] Navegação para `/app/atendimento/pedido` mostrada
- [ ] Retorno ao dashboard demonstrado
- [ ] Clique em card abre lista filtrada
- [ ] Duração entre 1 min e 1 min 30 s

---

## Arquivos relacionados

| Item | Caminho |
|---|---|
| Teste E2E | `automation/playwright/tests/dashboard/04-atalhos.spec.ts` |
| Vídeo gerado | `E:\videos-vendapps\...\video.webm` |
